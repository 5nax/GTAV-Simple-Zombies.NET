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
	public static float BlindStalkerChance = 0.10f;       // sound-only "clicker"-style zombie

	// --- Zombie density & behavior (realism: fewer, slower, deadlier) ---
	public static int MaxZombies = 14;                    // ambient cap near the player (was 30)
	public static int MinZombies = 4;
	public static int ZombieHealth = 350;
	public static int SpawnDistance = 90;                 // spawn further out, off-screen
	public static int MinSpawnDistance = 55;
	public static float RunnerChanceDay = 0.02f;          // most are shamblers by day
	public static float RunnerChanceNight = 0.18f;        // a few sprinters at night
	public static float SensingRange = 90f;               // how far a zombie can notice you at all
	public static float VisionDistance = 28f;             // line-of-sight detection range

	// --- Stealth & noise (gunfire/sprint draw the dead; crouch & suppressors hide you) ---
	public static bool StealthEnabled = true;
	public static float GunshotNoiseRadius = 90f;         // unsuppressed shots aggro within this
	public static float SuppressedNoiseRadius = 22f;
	public static float SprintNoiseRadius = 28f;

	// --- Injury / bleeding (higher stakes) ---
	public static bool BleedingEnabled = true;
	public static int BleedChancePercent = 45;            // chance a zombie hit opens a wound
	public static float BleedDamagePerSecond = 1.6f;

	// --- Hunting ---
	public static bool HuntingEnabled = true;

	// --- AI companions (Google Gemini) — paste your key in the INI to enable ---
	public static bool AiEnabled = false;
	public static string GeminiApiKey = "";
	public static string GeminiModel = "gemini-2.5-flash";
	public static bool AiTtsEnabled = true;            // NPCs speak via Windows SAPI
	public static bool AiVoiceInputEnabled = false;    // push-to-talk SAPI dictation (opt-in)
	public static float AiBarkIntervalSeconds = 45f;   // how often idle companions self-narrate

	// --- Player infection ---
	public static bool InfectionEnabled = true;
	public static int InfectionBiteChancePercent = 35;   // chance a zombie melee hit infects you
	public static float InfectionPerBite = 12f;          // infection % added per infecting hit
	public static float InfectionGrowthPerSec = 0.6f;    // passive growth once infected
	public static float InfectionDamageAtFull = 4f;      // health lost per damage tick at 100%

	// --- Horde / dynamic world (off by default — this is survival, not L4D) ---
	public static bool HordesEnabled = false;
	public static int HordeIntervalSeconds = 900;
	public static int HordeSize = 5;
	public static bool BloodMoonEnabled = true;
	public static int BloodMoonChancePercent = 8;        // rare, ominous

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
			BlindStalkerChance = s.GetValue("variants", "blind_stalker_chance", BlindStalkerChance);

			MaxZombies = s.GetValue("zombies", "max_nearby", MaxZombies);
			MinZombies = s.GetValue("zombies", "min_nearby", MinZombies);
			ZombieHealth = s.GetValue("zombies", "health", ZombieHealth);
			SpawnDistance = s.GetValue("zombies", "spawn_distance", SpawnDistance);
			MinSpawnDistance = s.GetValue("zombies", "min_spawn_distance", MinSpawnDistance);
			RunnerChanceDay = s.GetValue("zombies", "runner_chance_day", RunnerChanceDay);
			RunnerChanceNight = s.GetValue("zombies", "runner_chance_night", RunnerChanceNight);
			SensingRange = s.GetValue("zombies", "sensing_range", SensingRange);
			VisionDistance = s.GetValue("zombies", "vision_distance", VisionDistance);

			StealthEnabled = s.GetValue("stealth", "enabled", StealthEnabled);
			GunshotNoiseRadius = s.GetValue("stealth", "gunshot_noise_radius", GunshotNoiseRadius);
			SuppressedNoiseRadius = s.GetValue("stealth", "suppressed_noise_radius", SuppressedNoiseRadius);
			SprintNoiseRadius = s.GetValue("stealth", "sprint_noise_radius", SprintNoiseRadius);

			BleedingEnabled = s.GetValue("bleeding", "enabled", BleedingEnabled);
			BleedChancePercent = s.GetValue("bleeding", "chance_percent", BleedChancePercent);
			BleedDamagePerSecond = s.GetValue("bleeding", "damage_per_second", BleedDamagePerSecond);

			HuntingEnabled = s.GetValue("hunting", "enabled", HuntingEnabled);

			AiEnabled = s.GetValue("ai", "enabled", AiEnabled);
			GeminiApiKey = s.GetValue("ai", "gemini_api_key", GeminiApiKey);
			GeminiModel = s.GetValue("ai", "gemini_model", GeminiModel);
			AiTtsEnabled = s.GetValue("ai", "tts_enabled", AiTtsEnabled);
			AiVoiceInputEnabled = s.GetValue("ai", "voice_input_enabled", AiVoiceInputEnabled);
			AiBarkIntervalSeconds = s.GetValue("ai", "bark_interval_seconds", AiBarkIntervalSeconds);
			s.SetValue("ai", "enabled", AiEnabled);
			s.SetValue("ai", "gemini_api_key", GeminiApiKey);
			s.SetValue("ai", "gemini_model", GeminiModel);
			s.SetValue("ai", "tts_enabled", AiTtsEnabled);
			s.SetValue("ai", "voice_input_enabled", AiVoiceInputEnabled);
			s.SetValue("ai", "bark_interval_seconds", AiBarkIntervalSeconds);

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
