using System.Windows.Forms;
using GTA;
using GTA.Native;
using NativeUI;
using ZombiesMod.Controllers;
using ZombiesMod.Extensions;
using ZombiesMod.PlayerManagement;
using ZombiesMod.Scripts;
using ZombiesMod.Static;
using ZombiesMod.Zombies;

namespace ZombiesMod;

public class ModController : Script
{
	private Keys _menuKey = (Keys)121;

	public UIMenu MainMenu { get; private set; }

	public static ModController Instance { get; private set; }

	public ModController()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		Instance = this;
		Config.Check();
		Relationships.SetRelationships();
		LoadSave();
		ConfigureMenu();
		base.KeyUp += new KeyEventHandler(OnKeyUp);
	}

	private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (!MenuConrtoller.MenuPool.IsAnyMenuOpen() && keyEventArgs.KeyCode == _menuKey)
		{
			MainMenu.Visible = !MainMenu.Visible;
		}
	}

	private void LoadSave()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		_menuKey = base.Settings.GetValue<Keys>("keys", "zombies_menu_key", _menuKey);
		ZombiePed.ZombieDamage = base.Settings.GetValue("zombies", "zombie_damage", ZombiePed.ZombieDamage);
		base.Settings.SetValue<Keys>("keys", "zombies_menu_key", _menuKey);
		base.Settings.SetValue("zombies", "zombie_damage", ZombiePed.ZombieDamage);
		base.Settings.Save();
	}

	private void ConfigureMenu()
	{
		MainMenu = new UIMenu("Simple Zombies", "SELECT AN OPTION");
		MenuConrtoller.MenuPool.Add(MainMenu);
		UIMenuCheckboxItem uIMenuCheckboxItem = new UIMenuCheckboxItem("Infection Mode", check: false, "Enable/Disable zombies.");
		uIMenuCheckboxItem.CheckboxEvent += delegate(UIMenuCheckboxItem sender, bool @checked)
		{
			ZombieVehicleSpawner.Instance.Spawn = @checked;
			Loot247.Instance.Spawn = @checked;
			WorldController.Configure = @checked;
			AnimalSpawner.Instance.Spawn = @checked;
			if (@checked)
			{
				WorldExtended.ClearAreaOfEverything(Database.PlayerPosition, 10000f);
				Function.Call(Hash._0x41B4893843BBDB74, new InputArgument[1] { "cs3_07_mpgates" });
			}
		};
		UIMenuCheckboxItem uIMenuCheckboxItem2 = new UIMenuCheckboxItem("Fast Zombies", check: false, "Enable/Disable running zombies.");
		uIMenuCheckboxItem2.CheckboxEvent += delegate(UIMenuCheckboxItem sender, bool @checked)
		{
			ZombieCreator.Runners = @checked;
		};
		UIMenuCheckboxItem uIMenuCheckboxItem3 = new UIMenuCheckboxItem("Electricity", check: true, "Enables/Disable blackout mode.");
		uIMenuCheckboxItem3.CheckboxEvent += delegate(UIMenuCheckboxItem sender, bool @checked)
		{
			World.SetBlackout(!@checked);
		};
		UIMenuCheckboxItem uIMenuCheckboxItem4 = new UIMenuCheckboxItem("Survivors", check: false, "Enable/Disable survivors.");
		uIMenuCheckboxItem4.CheckboxEvent += delegate(UIMenuCheckboxItem sender, bool @checked)
		{
			SurvivorController.Instance.Spawn = @checked;
		};
		UIMenuCheckboxItem uIMenuCheckboxItem5 = new UIMenuCheckboxItem("Stats", check: true, "Enable/Disable stats.");
		uIMenuCheckboxItem5.CheckboxEvent += delegate(UIMenuCheckboxItem sender, bool @checked)
		{
			PlayerStats.UseStats = @checked;
		};
		UIMenuItem uIMenuItem = new UIMenuItem("Load", "Load the map, your vehicles and your bodyguards.");
		uIMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Heart);
		uIMenuItem.Activated += delegate
		{
			PlayerMap.Instance.Deserialize();
			PlayerVehicles.Instance.Deserialize();
			PlayerGroupManager.Instance.Deserialize();
		};
		UIMenuItem uIMenuItem2 = new UIMenuItem("Save", "Saves the vehicle you are currently in.");
		uIMenuItem2.SetLeftBadge(UIMenuItem.BadgeStyle.Car);
		uIMenuItem2.Activated += delegate
		{
			if (Database.PlayerCurrentVehicle == null || (Database.PlayerCurrentVehicle != null && !Database.PlayerCurrentVehicle.Exists()))
			{
				UI.Notify("You're not in a vehicle.");
			}
			else
			{
				PlayerVehicles.Instance.SaveVehicle(Database.PlayerCurrentVehicle);
			}
		};
		UIMenuItem uIMenuItem3 = new UIMenuItem("Save All", "Saves all marked vehicles, and their positions.");
		uIMenuItem3.SetLeftBadge(UIMenuItem.BadgeStyle.Car);
		uIMenuItem3.Activated += delegate
		{
			PlayerVehicles.Instance.Serialize(notify: true);
		};
		UIMenuItem uIMenuItem4 = new UIMenuItem("Save All", "Saves the player ped group (guards).");
		uIMenuItem4.SetLeftBadge(UIMenuItem.BadgeStyle.Mask);
		uIMenuItem4.Activated += delegate
		{
			PlayerGroupManager.Instance.SavePeds();
		};
		MainMenu.AddItem(uIMenuCheckboxItem);
		MainMenu.AddItem(uIMenuCheckboxItem2);
		MainMenu.AddItem(uIMenuCheckboxItem3);
		MainMenu.AddItem(uIMenuCheckboxItem4);
		MainMenu.AddItem(uIMenuCheckboxItem5);
		MainMenu.AddItem(uIMenuItem);
		MainMenu.AddItem(uIMenuItem2);
		MainMenu.AddItem(uIMenuItem3);
		MainMenu.AddItem(uIMenuItem4);
		MainMenu.RefreshIndex();
	}
}
