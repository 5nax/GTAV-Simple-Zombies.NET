using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI.Menus;
using ZombiesMod.Controllers;
using ZombiesMod.Extensions;
using ZombiesMod.PlayerManagement;
using ZombiesMod.Scripts;
using ZombiesMod.Static;
using ZombiesMod.Zombies;

namespace ZombiesMod;

public class ModController : Script
{
	private Keys _menuKey = Keys.F10;

	public NativeMenu MainMenu { get; private set; }

	public static ModController Instance { get; private set; }

	public ModController()
	{
		Instance = this;
		Config.Check();
		Relationships.SetRelationships();
		LoadSave();
		ConfigureMenu();
		KeyUp += OnKeyUp;
	}

	private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
	{
		if (!MenuConrtoller.MenuPool.AreAnyVisible && keyEventArgs.KeyCode == _menuKey)
		{
			MainMenu.Visible = !MainMenu.Visible;
		}
	}

	private void LoadSave()
	{
		_menuKey = Settings.GetValue("keys", "zombies_menu_key", _menuKey);
		ZombiePed.ZombieDamage = Settings.GetValue("zombies", "zombie_damage", ZombiePed.ZombieDamage);
		Settings.SetValue("keys", "zombies_menu_key", _menuKey);
		Settings.SetValue("zombies", "zombie_damage", ZombiePed.ZombieDamage);
		Settings.Save();
	}

	private void ConfigureMenu()
	{
		MainMenu = new NativeMenu("Simple Zombies", "SELECT AN OPTION");
		MenuConrtoller.MenuPool.Add(MainMenu);

		NativeCheckboxItem infection = new NativeCheckboxItem("Infection Mode", "Enable/Disable zombies.", false);
		infection.CheckboxChanged += delegate
		{
			bool @checked = infection.Checked;
			ZombieVehicleSpawner.Instance.Spawn = @checked;
			Loot247.Instance.Spawn = @checked;
			WorldController.Configure = @checked;
			AnimalSpawner.Instance.Spawn = @checked;
			if (@checked)
			{
				WorldExtended.ClearAreaOfEverything(Database.PlayerPosition, 10000f);
				Function.Call((Hash)0x41B4893843BBDB74uL, "cs3_07_mpgates");
			}
		};

		NativeCheckboxItem fastZombies = new NativeCheckboxItem("Fast Zombies", "Enable/Disable running zombies.", false);
		fastZombies.CheckboxChanged += delegate
		{
			ZombieCreator.Runners = fastZombies.Checked;
		};

		NativeCheckboxItem electricity = new NativeCheckboxItem("Electricity", "Enables/Disable blackout mode.", true);
		electricity.CheckboxChanged += delegate
		{
			World.Blackout = !electricity.Checked;
		};

		NativeCheckboxItem survivors = new NativeCheckboxItem("Survivors", "Enable/Disable survivors.", false);
		survivors.CheckboxChanged += delegate
		{
			SurvivorController.Instance.Spawn = survivors.Checked;
		};

		NativeCheckboxItem stats = new NativeCheckboxItem("Stats", "Enable/Disable stats.", true);
		stats.CheckboxChanged += delegate
		{
			PlayerStats.UseStats = stats.Checked;
		};

		NativeCheckboxItem hordes = new NativeCheckboxItem("Hordes", "Periodic zombie hordes converge on you.", GameConfig.HordesEnabled);
		hordes.CheckboxChanged += delegate
		{
			GameConfig.HordesEnabled = hordes.Checked;
		};

		NativeCheckboxItem bloodMoon = new NativeCheckboxItem("Blood Moons", "Random nights become a frenzied blood moon.", GameConfig.BloodMoonEnabled);
		bloodMoon.CheckboxChanged += delegate
		{
			GameConfig.BloodMoonEnabled = bloodMoon.Checked;
		};

		NativeCheckboxItem playerInfection = new NativeCheckboxItem("Player Infection", "Zombie bites can infect you; craft an Antidote to cure.", GameConfig.InfectionEnabled);
		playerInfection.CheckboxChanged += delegate
		{
			GameConfig.InfectionEnabled = playerInfection.Checked;
		};

		NativeCheckboxItem fuel = new NativeCheckboxItem("Vehicle Fuel", "Vehicles burn fuel; refuel at pumps or with a Fuel Can.", GameConfig.FuelEnabled);
		fuel.CheckboxChanged += delegate
		{
			GameConfig.FuelEnabled = fuel.Checked;
		};

		NativeItem load = new NativeItem("Load", "Load the map, your vehicles and your bodyguards.");
		load.Activated += delegate
		{
			PlayerMap.Instance.Deserialize();
			PlayerVehicles.Instance.Deserialize();
			PlayerGroupManager.Instance.Deserialize();
		};

		NativeItem saveVehicle = new NativeItem("Save", "Saves the vehicle you are currently in.");
		saveVehicle.Activated += delegate
		{
			if (Database.PlayerCurrentVehicle == null || !Database.PlayerCurrentVehicle.Exists())
			{
				GTA.UI.Notification.PostTicker("You're not in a vehicle.", false, true);
			}
			else
			{
				PlayerVehicles.Instance.SaveVehicle(Database.PlayerCurrentVehicle);
			}
		};

		NativeItem saveAllVehicles = new NativeItem("Save All Vehicles", "Saves all marked vehicles, and their positions.");
		saveAllVehicles.Activated += delegate
		{
			PlayerVehicles.Instance.Serialize(notify: true);
		};

		NativeItem saveGuards = new NativeItem("Save Guards", "Saves the player ped group (guards).");
		saveGuards.Activated += delegate
		{
			PlayerGroupManager.Instance.SavePeds();
		};

		MainMenu.Add(infection);
		MainMenu.Add(fastZombies);
		MainMenu.Add(electricity);
		MainMenu.Add(survivors);
		MainMenu.Add(stats);
		MainMenu.Add(hordes);
		MainMenu.Add(bloodMoon);
		MainMenu.Add(playerInfection);
		MainMenu.Add(fuel);
		MainMenu.Add(load);
		MainMenu.Add(saveVehicle);
		MainMenu.Add(saveAllVehicles);
		MainMenu.Add(saveGuards);
	}
}
