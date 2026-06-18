using GTA;

namespace ZombiesMod.Static;

// Central, INI-backed tunables for all the new systems (variants, infection, hordes,
// progression, fuel, combat). Loaded once from scripts/ZombiesMod.ini and written back
// so every knob is visible/editable. Keep defaults sane so first run is playable.
public static class GameConfig
{
	// --- Special zombie variant chances (rolled per infected ped; sum should stay < 1) ---
	public static float BruteChance = 0.05f;
	public static float CrawlerChance = 0.06f;
	public static float BloaterChance = 0.03f;
	public static float SpitterChance = 0.03f;
	public static float ScreamerChance = 0.02f;

	// --- Player infection ---
	public static bool InfectionEnabled = true;
	public static int InfectionBiteChancePercent = 35;   // chance a zombie melee hit infects you
	public static float InfectionPerBite = 12f;          // infection % added per infecting hit
	public static float InfectionGrowthPerSec = 0.6f;    // passive growth once infected
	public static float InfectionDamageAtFull = 4f;      // health lost per damage tick at 100%

	// --- Horde / dynamic world ---
	public static bool HordesEnabled = true;
	public static int HordeIntervalSeconds = 540;        // ~9 min between ambient hordes
	public static int HordeSize = 8;
	public static bool BloodMoonEnabled = true;
	public static int BloodMoonChancePercent = 20;       // chance each night becomes a blood moon

	// --- Progression ---
	public static bool ProgressionEnabled = true;
	public static int XpPerKill = 10;
	public static int XpPerSpecialKill = 30;

	// --- Vehicle fuel ---
	public static bool FuelEnabled = true;
	public static float FuelCapacity = 100f;
	public static float FuelDrainPerSecond = 0.18f;      // at full throttle
	public static float FuelFromCan = 50f;

	// --- Combat ---
	public static bool HeadshotBonusEnabled = true;
	public static float HeadshotDamageMultiplier = 2.5f;
	public static bool CorpseCleanupEnabled = true;
	public static int MaxCorpses = 25;

	static GameConfig()
	{
		Load();
	}

	public static void Load()
	{
		try
		{
			ScriptSettings s = ScriptSettings.Load(Config.IniFilePath);

			BruteChance = s.GetValue("variants", "brute_chance", BruteChance);
			CrawlerChance = s.GetValue("variants", "crawler_chance", CrawlerChance);
			BloaterChance = s.GetValue("variants", "bloater_chance", BloaterChance);
			SpitterChance = s.GetValue("variants", "spitter_chance", SpitterChance);
			ScreamerChance = s.GetValue("variants", "screamer_chance", ScreamerChance);

			InfectionEnabled = s.GetValue("infection", "enabled", InfectionEnabled);
			InfectionBiteChancePercent = s.GetValue("infection", "bite_chance_percent", InfectionBiteChancePercent);
			InfectionPerBite = s.GetValue("infection", "per_bite", InfectionPerBite);
			InfectionGrowthPerSec = s.GetValue("infection", "growth_per_sec", InfectionGrowthPerSec);
			InfectionDamageAtFull = s.GetValue("infection", "damage_at_full", InfectionDamageAtFull);

			HordesEnabled = s.GetValue("hordes", "enabled", HordesEnabled);
			HordeIntervalSeconds = s.GetValue("hordes", "interval_seconds", HordeIntervalSeconds);
			HordeSize = s.GetValue("hordes", "size", HordeSize);
			BloodMoonEnabled = s.GetValue("hordes", "blood_moon_enabled", BloodMoonEnabled);
			BloodMoonChancePercent = s.GetValue("hordes", "blood_moon_chance_percent", BloodMoonChancePercent);

			ProgressionEnabled = s.GetValue("progression", "enabled", ProgressionEnabled);
			XpPerKill = s.GetValue("progression", "xp_per_kill", XpPerKill);
			XpPerSpecialKill = s.GetValue("progression", "xp_per_special_kill", XpPerSpecialKill);

			FuelEnabled = s.GetValue("fuel", "enabled", FuelEnabled);
			FuelCapacity = s.GetValue("fuel", "capacity", FuelCapacity);
			FuelDrainPerSecond = s.GetValue("fuel", "drain_per_second", FuelDrainPerSecond);
			FuelFromCan = s.GetValue("fuel", "refuel_from_can", FuelFromCan);

			HeadshotBonusEnabled = s.GetValue("combat", "headshot_bonus_enabled", HeadshotBonusEnabled);
			HeadshotDamageMultiplier = s.GetValue("combat", "headshot_multiplier", HeadshotDamageMultiplier);
			CorpseCleanupEnabled = s.GetValue("combat", "corpse_cleanup_enabled", CorpseCleanupEnabled);
			MaxCorpses = s.GetValue("combat", "max_corpses", MaxCorpses);

			// Persist so all knobs appear in the INI for the player to tweak.
			s.SetValue("variants", "brute_chance", BruteChance);
			s.SetValue("variants", "crawler_chance", CrawlerChance);
			s.SetValue("variants", "bloater_chance", BloaterChance);
			s.SetValue("variants", "spitter_chance", SpitterChance);
			s.SetValue("variants", "screamer_chance", ScreamerChance);
			s.SetValue("infection", "enabled", InfectionEnabled);
			s.SetValue("infection", "bite_chance_percent", InfectionBiteChancePercent);
			s.SetValue("infection", "per_bite", InfectionPerBite);
			s.SetValue("infection", "growth_per_sec", InfectionGrowthPerSec);
			s.SetValue("infection", "damage_at_full", InfectionDamageAtFull);
			s.SetValue("hordes", "enabled", HordesEnabled);
			s.SetValue("hordes", "interval_seconds", HordeIntervalSeconds);
			s.SetValue("hordes", "size", HordeSize);
			s.SetValue("hordes", "blood_moon_enabled", BloodMoonEnabled);
			s.SetValue("hordes", "blood_moon_chance_percent", BloodMoonChancePercent);
			s.SetValue("progression", "enabled", ProgressionEnabled);
			s.SetValue("progression", "xp_per_kill", XpPerKill);
			s.SetValue("progression", "xp_per_special_kill", XpPerSpecialKill);
			s.SetValue("fuel", "enabled", FuelEnabled);
			s.SetValue("fuel", "capacity", FuelCapacity);
			s.SetValue("fuel", "drain_per_second", FuelDrainPerSecond);
			s.SetValue("fuel", "refuel_from_can", FuelFromCan);
			s.SetValue("combat", "headshot_bonus_enabled", HeadshotBonusEnabled);
			s.SetValue("combat", "headshot_multiplier", HeadshotDamageMultiplier);
			s.SetValue("combat", "corpse_cleanup_enabled", CorpseCleanupEnabled);
			s.SetValue("combat", "max_corpses", MaxCorpses);
			s.Save();
		}
		catch
		{
			// Fall back to defaults if the INI can't be read/written.
		}
	}
}
