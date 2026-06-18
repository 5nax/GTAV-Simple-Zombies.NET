using System;
using System.Drawing;
using System.Linq;
using GTA;
using LemonUI.TimerBars;
using ZombiesMod.Static;

namespace ZombiesMod.PlayerManagement;

// Player infection: zombie melee hits can infect you. Once infected the meter climbs
// over time; at 100% you take periodic damage (turning). Cure it with a crafted Antidote
// (ItemEvent.CureInfection). Shows a HUD bar only while infected.
public class PlayerInfection : Script
{
	public static PlayerInfection Instance { get; private set; }

	public static float Infection { get; private set; }

	private readonly TimerBarProgress _bar;

	private bool _barShown;

	private float _damageTimer;

	private int _nextBiteTime;

	private static Ped PlayerPed => Database.PlayerPed;

	public PlayerInfection()
	{
		Instance = this;
		_bar = new TimerBarProgress("INFECTION")
		{
			ForegroundColor = Color.FromArgb(120, 180, 60),
			BackgroundColor = Color.FromArgb(60, 30, 30)
		};
		Tick += OnTick;
		Interval = 0;
	}

	public void Cure(float amount)
	{
		Infection = Math.Max(0f, Infection - amount);
		Notifier.Show("~g~Antidote~s~ administered — infection reduced.");
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!GameConfig.InfectionEnabled || Database.PlayerIsDead)
		{
			HideBar();
			return;
		}

		DetectBites();

		if (Infection > 0f)
		{
			Infection = Math.Min(100f, Infection + GameConfig.InfectionGrowthPerSec * Game.LastFrameTime);
			ShowBar();
			_bar.Progress = Infection;
			if (Infection >= 100f)
			{
				_damageTimer += Game.LastFrameTime;
				if (_damageTimer >= 1f)
				{
					_damageTimer = 0f;
					PlayerPed.Health -= (int)GameConfig.InfectionDamageAtFull;
					GTA.UI.Screen.ShowSubtitle("~r~You are turning... find an Antidote!", 1200);
				}
			}
		}
		else
		{
			HideBar();
		}
	}

	// Register an infecting hit when a nearby infected ped has damaged the player.
	private void DetectBites()
	{
		Ped player = PlayerPed;
		if (player == null || !player.Exists() || Game.GameTime < _nextBiteTime)
		{
			return;
		}
		Ped[] nearby = World.GetNearbyPeds(player, 3f);
		Ped attacker = nearby.FirstOrDefault((Ped p) =>
			p != null && p.Exists() && !p.IsPlayer
			&& p.RelationshipGroup == Relationships.InfectedRelationship
			&& player.HasBeenDamagedBy(p));
		if (attacker == null)
		{
			return;
		}
		// Cooldown so a single melee streak rolls once; continued exposure ramps it up.
		_nextBiteTime = Game.GameTime + 1500;
		if (Database.Random.Next(100) < GameConfig.InfectionBiteChancePercent)
		{
			bool wasClean = Infection <= 0f;
			Infection = Math.Min(100f, Infection + GameConfig.InfectionPerBite);
			if (wasClean)
			{
				Notifier.Show("~r~You've been infected!~s~ Craft an ~g~Antidote~s~.", blinking: true);
			}
		}
	}

	private void ShowBar()
	{
		if (!_barShown)
		{
			MenuConrtoller.BarPool.Add(_bar);
			_barShown = true;
		}
	}

	private void HideBar()
	{
		if (_barShown)
		{
			MenuConrtoller.BarPool.Remove(_bar);
			_barShown = false;
		}
	}
}
