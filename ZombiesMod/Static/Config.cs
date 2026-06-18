using System.IO;
using GTA;

namespace ZombiesMod.Static;

public class Config
{
	public static string VersionId = "2.0.0-enhanced";

	public const string ScriptFilePath = "./scripts/";

	public const string IniFilePath = "./scripts/ZombiesMod.ini";

	public const string InventoryFilePath = "./scripts/Inventory.dat";

	public const string MapFilePath = "./scripts/Map.dat";

	public const string VehicleFilePath = "./scripts/Vehicles.dat";

	public const string GuardsFilePath = "./scripts/Guards.dat";

	public static void Check()
	{
		ScriptSettings scriptSettings = ScriptSettings.Load("./scripts/ZombiesMod.ini");
		if (!(scriptSettings.GetValue("mod", "version_id", "0") == VersionId))
		{
			if (File.Exists("./scripts/ZombiesMod.ini"))
			{
				File.Delete("./scripts/ZombiesMod.ini");
			}
			if (File.Exists(Config.InventoryFilePath))
			{
				File.Delete(Config.InventoryFilePath);
			}
			Notifier.Show($"Updating Simple Zombies to version ~g~{VersionId}~s~. Overwritting the " + "inventory file since there are new items.");
			scriptSettings.SetValue("mod", "version_id", VersionId);
			scriptSettings.Save();
		}
	}
}
