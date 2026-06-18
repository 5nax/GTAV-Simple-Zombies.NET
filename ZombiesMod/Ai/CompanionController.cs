using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using ZombiesMod.Extensions;
using ZombiesMod.Static;

namespace ZombiesMod.Ai;

// A living survivor companion backed by Gemini. Knows the situation (zombies, place,
// time, weather, its health, its current order), talks back via TTS, and acts on what
// you tell it. Talk by typing (talk key) or by voice (voice key, push-to-talk).
public class AiCompanion
{
	public Ped Ped;
	public string Name;
	public string Persona;
	public string CurrentOrder = "sticking with the leader";
	public int NextBarkTime;
	public bool Thinking;

	public AiCompanion(Ped ped, string name, string persona)
	{
		Ped = ped;
		Name = name;
		Persona = persona;
	}
}

public class CompanionController : Script
{
	private class AiResult
	{
		public AiCompanion Companion;
		public AiDecision Decision;
	}

	private static readonly string[] Names =
	{
		"Mara", "Cole", "Tess", "Ramos", "Jess", "Vince", "Lena", "Dwight", "Quinn", "Sara", "Hadley", "Boon"
	};

	private static readonly string[] Personas =
	{
		"a level-headed ex-paramedic who keeps everyone calm",
		"a jumpy but loyal former mechanic with a dark sense of humor",
		"a hardened ex-soldier, short on words, quick on the trigger",
		"a resourceful scavenger who knows every shortcut in the city",
		"a soft-spoken teacher trying to hold onto their humanity"
	};

	public static CompanionController Instance { get; private set; }

	private readonly List<AiCompanion> _companions = new List<AiCompanion>();

	private readonly ConcurrentQueue<AiResult> _results = new ConcurrentQueue<AiResult>();

	private Keys _talkKey;

	private Keys _voiceKey;

	private AiCompanion _voiceTarget;

	private int _listenUntil;

	private int _nextFendTick;

	private bool _speechInit;

	private static Ped PlayerPed => Database.PlayerPed;

	public CompanionController()
	{
		Instance = this;
		_talkKey = Settings.GetValue("keys", "companion_talk_key", Keys.B);
		_voiceKey = Settings.GetValue("keys", "companion_voice_key", Keys.V);
		Settings.SetValue("keys", "companion_talk_key", _talkKey);
		Settings.SetValue("keys", "companion_voice_key", _voiceKey);
		Settings.Save();
		Tick += OnTick;
		KeyUp += OnKeyUp;
		Aborted += delegate { _companions.Clear(); };
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!GeminiClient.IsConfigured)
		{
			return; // AI stays dormant until a key is configured
		}
		if (!_speechInit)
		{
			AiSpeech.EnsureInit();
			_speechInit = true;
		}

		_companions.RemoveAll((AiCompanion c) => c.Ped == null || !c.Ped.Exists() || c.Ped.IsDead);

		while (_results.TryDequeue(out AiResult r))
		{
			ApplyResult(r);
		}

		HandleVoiceWindow();

		if (Game.GameTime >= _nextFendTick)
		{
			_nextFendTick = Game.GameTime + 3000;
			FendForThemselves();
		}

		MaybeBark();
	}

	// Unprompted in-character chatter so companions feel alive — throttled per companion
	// and only when they're near you, so it stays cheap and relevant.
	private void MaybeBark()
	{
		AiCompanion c = _companions.FirstOrDefault((AiCompanion x) =>
			!x.Thinking && Game.GameTime >= x.NextBarkTime
			&& x.Ped != null && x.Ped.Exists() && !x.Ped.IsDead
			&& x.Ped.Position.DistanceTo(PlayerPed.Position) < 25f);
		if (c == null)
		{
			return;
		}
		c.NextBarkTime = Game.GameTime + (int)(GameConfig.AiBarkIntervalSeconds * 1000f) + Database.Random.Next(0, 20000);
		c.Thinking = true;
		string system = BuildSystemPrompt(c)
			+ " This is an unprompted moment: say ONE brief, natural line reacting to the current situation "
			+ "(a warning, a worry, a quip). Keep action \"none\" unless immediate danger demands otherwise.";
		string context = BuildContext(c, "(no direct order — just react to the moment out loud)");
		AiCompanion captured = c;
		Task.Run(async delegate
		{
			string json = await GeminiClient.GenerateAsync(system, context).ConfigureAwait(false);
			_results.Enqueue(new AiResult { Companion = captured, Decision = GeminiClient.ParseDecision(json) });
		});
	}

	private void HandleVoiceWindow()
	{
		if (!AiSpeech.Listening)
		{
			return;
		}
		if (Game.GameTime > _listenUntil)
		{
			AiSpeech.Listening = false;
			return;
		}
		if (AiSpeech.TryGetHeard(out string heard) && _voiceTarget != null)
		{
			AiSpeech.Listening = false;
			Send(_voiceTarget, heard);
		}
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (!GeminiClient.IsConfigured || Database.PlayerIsDead || MenuConrtoller.MenuPool.AreAnyVisible)
		{
			return;
		}
		if (e.KeyCode == _talkKey)
		{
			AiCompanion c = DesignateNearest();
			if (c == null)
			{
				return;
			}
			string msg = Game.GetUserInput(WindowTitle.EnterMessage60, "", 140);
			if (!string.IsNullOrWhiteSpace(msg))
			{
				Send(c, msg);
			}
		}
		else if (e.KeyCode == _voiceKey)
		{
			if (!AiSpeech.HasVoiceInput)
			{
				Notifier.Show("Voice input is off — enable [ai] voice_input_enabled in the INI.");
				return;
			}
			AiCompanion c = DesignateNearest();
			if (c == null)
			{
				return;
			}
			_voiceTarget = c;
			AiSpeech.ClearHeard();
			AiSpeech.Listening = true;
			_listenUntil = Game.GameTime + 6000;
			Notifier.Show($"~b~Listening~s~ — speak to {c.Name}.");
		}
	}

	// Find (or adopt) the nearest living human ally to talk to.
	private AiCompanion DesignateNearest()
	{
		Ped player = PlayerPed;
		Ped near = World.GetNearbyPeds(player, 9f)
			.Where((Ped p) => p != null && p.Exists() && !p.IsPlayer && p.IsHuman && !p.IsDead
				&& p.RelationshipGroup != Relationships.InfectedRelationship)
			.OrderBy((Ped p) => p.Position.DistanceTo(player.Position))
			.FirstOrDefault();
		if (near == null)
		{
			Notifier.Show("No survivor nearby to talk to.");
			return null;
		}
		AiCompanion existing = _companions.FirstOrDefault((AiCompanion c) => c.Ped == near);
		if (existing != null)
		{
			return existing;
		}
		string name = Names[Database.Random.Next(Names.Length)];
		string persona = Personas[Database.Random.Next(Personas.Length)];
		AiCompanion comp = new AiCompanion(near, name, persona);
		comp.NextBarkTime = Game.GameTime + (int)(GameConfig.AiBarkIntervalSeconds * 1000f);
		near.Recruit(player);
		near.IsPersistent = true;
		_companions.Add(comp);
		Notifier.Show($"~g~{name}~s~ — {persona} — is with you.");
		return comp;
	}

	private void Send(AiCompanion c, string playerMessage)
	{
		if (c.Thinking)
		{
			return; // one request at a time per companion
		}
		c.Thinking = true;
		Notifier.Show($"~b~You:~s~ {playerMessage}");
		string system = BuildSystemPrompt(c);
		string context = BuildContext(c, playerMessage);
		AiCompanion captured = c;
		Task.Run(async delegate
		{
			string json = await GeminiClient.GenerateAsync(system, context).ConfigureAwait(false);
			_results.Enqueue(new AiResult { Companion = captured, Decision = GeminiClient.ParseDecision(json) });
		});
	}

	private void ApplyResult(AiResult r)
	{
		AiCompanion c = r.Companion;
		c.Thinking = false;
		if (c.Ped == null || !c.Ped.Exists())
		{
			return;
		}
		if (r.Decision == null || (string.IsNullOrWhiteSpace(r.Decision.Say) && r.Decision.Action == "none"))
		{
			GTA.UI.Screen.ShowSubtitle($"~y~{c.Name}~s~ doesn't respond. (Check ZombiesAi.log / your API key)", 3500);
			return;
		}
		if (!string.IsNullOrWhiteSpace(r.Decision.Say))
		{
			GTA.UI.Screen.ShowSubtitle($"~y~{c.Name}:~s~ {r.Decision.Say}", 6000);
			AiSpeech.Speak(r.Decision.Say);
		}
		ExecuteAction(c, r.Decision.Action);
	}

	private void ExecuteAction(AiCompanion c, string action)
	{
		Ped ped = c.Ped;
		Ped player = PlayerPed;
		switch ((action ?? "none").Trim().ToLowerInvariant())
		{
		case "follow":
		case "regroup":
		case "comewith":
			ped.Recruit(player);
			ped.Task.GoTo(player);
			c.CurrentOrder = "following the leader";
			break;
		case "hold":
		case "stay":
		case "guard":
			ped.Task.GuardCurrentPosition();
			c.CurrentOrder = "holding position";
			break;
		case "attack":
		case "fight":
		case "engage":
			ped.Task.FightAgainstHatedTargets(120f);
			c.CurrentOrder = "fighting the dead";
			break;
		case "hunt":
			HuntNearestAnimal(ped);
			c.CurrentOrder = "hunting for food";
			break;
		case "build":
		case "fortify":
		case "barricade":
			BuildCover(ped);
			c.CurrentOrder = "building defenses";
			break;
		case "flee":
		case "retreat":
			Ped threat = NearestZombie(ped.Position, 40f);
			if (threat != null)
			{
				ped.Task.ReactAndFlee(threat);
			}
			c.CurrentOrder = "retreating";
			break;
		case "wander":
		case "patrol":
		case "scavenge":
			ped.Task.WanderAround();
			c.CurrentOrder = "scavenging the area";
			break;
		default:
			break; // "none"/"talk": no order change
		}
	}

	// Drops a piece of cover in front of the companion — a tangible "build" action.
	private static void BuildCover(Ped ped)
	{
		Vector3 spot = ped.Position + ped.ForwardVector * 1.6f;
		ped.Task.PlayAnimation("amb@world_human_gardener_plant@male@base", "base", 8f, 2500, AnimationFlags.UpperBodyOnly);
		Prop cover = World.CreateProp("prop_mb_sandblock_02", spot, new Vector3(0f, 0f, ped.Heading), dynamic: false, placeOnGround: true);
		if (cover != null)
		{
			cover.IsPositionFrozen = true;
			cover.IsPersistent = true;
		}
	}

	private static void HuntNearestAnimal(Ped ped)
	{
		Ped animal = World.GetNearbyPeds(ped, 90f)
			.Where((Ped p) => p != null && p.Exists() && !p.IsDead && !p.IsHuman && !p.IsPlayer)
			.OrderBy((Ped p) => p.Position.DistanceTo(ped.Position))
			.FirstOrDefault();
		if (animal != null)
		{
			ped.Task.GoTo(animal);
		}
		else
		{
			ped.Task.WanderAround();
		}
	}

	// Even without orders, companions fight off zombies that get close.
	private void FendForThemselves()
	{
		foreach (AiCompanion c in _companions)
		{
			if (c.Ped == null || !c.Ped.Exists() || c.Ped.IsDead || c.CurrentOrder == "holding position")
			{
				continue;
			}
			if (NearestZombie(c.Ped.Position, 18f) != null && !c.Ped.IsInCombat)
			{
				c.Ped.Task.FightAgainstHatedTargets(60f);
			}
		}
	}

	private static Ped NearestZombie(Vector3 from, float radius)
	{
		return World.GetNearbyPeds(from, radius)
			.Where((Ped p) => p != null && p.Exists() && !p.IsDead && p.RelationshipGroup == Relationships.InfectedRelationship)
			.OrderBy((Ped p) => p.Position.DistanceTo(from))
			.FirstOrDefault();
	}

	private static string BuildSystemPrompt(AiCompanion c)
	{
		return $"You are {c.Name}, {c.Persona}. You are a survivor companion to the player (your leader) "
			+ "during a zombie apocalypse in Los Santos (the GTA V map). Stay fully in character: have "
			+ "opinions, fear, humor and loyalty — feel alive. You are aware of your surroundings from the "
			+ "situation report. Reply ONLY as compact JSON on a single line: "
			+ "{\"say\":\"<one or two SHORT sentences, spoken aloud>\",\"action\":\"<follow|hold|attack|hunt|build|flee|wander|none>\",\"target\":\"\"}. "
			+ "Pick the action that best fits what the leader asked and the danger around you. Never narrate the JSON.";
	}

	private static string BuildContext(AiCompanion c, string playerMessage)
	{
		Ped ped = c.Ped;
		Ped player = PlayerPed;
		Vector3 pos = ped.Position;
		int hour = World.CurrentTimeOfDay.Hours;
		string partOfDay = (hour >= 20 || hour <= 5) ? "night" : (hour < 12 ? "morning" : "afternoon");
		int zombies = World.GetNearbyPeds(ped, 60f)
			.Count((Ped p) => p != null && p.Exists() && !p.IsDead && p.RelationshipGroup == Relationships.InfectedRelationship);
		Ped nearest = NearestZombie(pos, 60f);
		string nearestStr = nearest != null ? $" (closest about {(int)nearest.Position.DistanceTo(pos)}m away)" : "";
		int hp = ped.MaxHealth > 0 ? (int)(100f * ped.Health / ped.MaxHealth) : 100;
		string zone = World.GetZoneLocalizedName(pos);
		int leaderDist = (int)pos.DistanceTo(player.Position);

		StringBuilder sb = new StringBuilder();
		sb.Append($"Situation report — it is {partOfDay} ({hour}:00), weather {World.Weather}. ");
		sb.Append($"You ({c.Name}) are in {zone}, health {hp}%. ");
		sb.Append(zombies == 0 ? "No zombies nearby right now. " : $"{zombies} zombie(s) within 60m{nearestStr}. ");
		sb.Append($"Your current order: {c.CurrentOrder}. The leader is {leaderDist}m from you.\n");
		sb.Append($"The leader says to you: \"{playerMessage}\"\n");
		sb.Append("Respond in character as the JSON described.");
		return sb.ToString();
	}
}
