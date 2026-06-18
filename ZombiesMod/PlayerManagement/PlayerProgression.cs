using System;
using System.Drawing;
using GTA;
using GTA.UI;
using ZombiesMod.Static;
using ZombiesMod.Zombies;
using ZombiesMod.Zombies.ZombieTypes;
using Font = GTA.UI.Font;

namespace ZombiesMod.PlayerManagement;

// Survival progression: counts zombie kills, tracks days survived, awards XP, levels
// up, and grants escalating perks (more max health, tougher player). Draws a compact
// HUD and persists to scripts/ZombiesProgress.ini.
public class PlayerProgression : Script
{
	private const string SaveFile = "./scripts/ZombiesProgress.ini";

	public static PlayerProgression Instance { get; private set; }

	public static int Kills { get; private set; }

	public static int Level { get; private set; } = 1;

	public static int Xp { get; private set; }

	public static int DaysSurvived { get; private set; }

	private int _xpToNext = 100;

	private int _lastDayOfYear = -1;

	public PlayerProgression()
	{
		Instance = this;
		LoadProgress();
		ZombiePed.Killed += OnZombieKilled;
		Tick += OnTick;
		Aborted += delegate { SaveProgress(); };
	}

	private void OnZombieKilled(ZombiePed zombie)
	{
		if (!GameConfig.ProgressionEnabled || zombie == null)
		{
			return;
		}
		Ped ped = zombie;
		if (ped == null || !ped.HasBeenDamagedBy(Database.PlayerPed))
		{
			return;
		}
		Kills++;
		bool special = !(zombie is Runner) && !(zombie is Walker);
		AddXp(special ? GameConfig.XpPerSpecialKill : GameConfig.XpPerKill);
		SaveProgress();
	}

	private void AddXp(int amount)
	{
		Xp += amount;
		while (Xp >= _xpToNext)
		{
			Xp -= _xpToNext;
			Level++;
			_xpToNext = 100 + (Level - 1) * 75;
			ApplyLevelPerks();
			Notifier.Show($"~g~Level Up!~s~ You are now level ~y~{Level}~s~.", blinking: true);
		}
	}

	// Each level toughens the player a little (and tops them off).
	private void ApplyLevelPerks()
	{
		Ped player = Database.PlayerPed;
		if (player == null || !player.Exists())
		{
			return;
		}
		int target = 100 + (Level - 1) * 10;
		player.MaxHealth = target;
		player.Health = target;
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!GameConfig.ProgressionEnabled)
		{
			return;
		}
		// Count a survived day whenever the in-game date rolls over.
		int doy = World.CurrentDate.DayOfYear;
		if (_lastDayOfYear == -1)
		{
			_lastDayOfYear = doy;
		}
		else if (doy != _lastDayOfYear)
		{
			_lastDayOfYear = doy;
			DaysSurvived++;
			SaveProgress();
		}

		if (!MenuConrtoller.MenuPool.AreAnyVisible && !Database.PlayerIsDead)
		{
			DrawHud();
		}
	}

	private void DrawHud()
	{
		new TextElement($"~p~Day {DaysSurvived}", new PointF(15f, 5f), 0.45f, Color.White, Font.ChaletLondon, Alignment.Left, shadow: true, outline: false).Draw();
		new TextElement($"Lvl {Level}  ~g~{Kills}~s~ kills  ~b~{Xp}~s~/{_xpToNext} xp", new PointF(15f, 28f), 0.35f, Color.White, Font.ChaletLondon, Alignment.Left, shadow: true, outline: false).Draw();
	}

	private void LoadProgress()
	{
		try
		{
			ScriptSettings s = ScriptSettings.Load(SaveFile);
			Kills = s.GetValue("progress", "kills", 0);
			Level = Math.Max(1, s.GetValue("progress", "level", 1));
			Xp = s.GetValue("progress", "xp", 0);
			DaysSurvived = s.GetValue("progress", "days", 0);
			_xpToNext = 100 + (Level - 1) * 75;
		}
		catch
		{
		}
	}

	private void SaveProgress()
	{
		try
		{
			ScriptSettings s = ScriptSettings.Load(SaveFile);
			s.SetValue("progress", "kills", Kills);
			s.SetValue("progress", "level", Level);
			s.SetValue("progress", "xp", Xp);
			s.SetValue("progress", "days", DaysSurvived);
			s.Save();
		}
		catch
		{
		}
	}
}
