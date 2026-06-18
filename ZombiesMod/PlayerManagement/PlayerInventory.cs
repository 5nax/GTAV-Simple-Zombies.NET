using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Menus;
using ZombiesMod.DataClasses;
using ZombiesMod.Extensions;
using ZombiesMod.Static;
using Control = GTA.Control;

namespace ZombiesMod.PlayerManagement;

public class PlayerInventory : Script
{
	public delegate void OnUsedFoodEvent(FoodInventoryItem item, FoodType foodType);

	public delegate void OnUsedWeaponEvent(WeaponInventoryItem weapon);

	public delegate void OnUsedBuildableEvent(BuildableInventoryItem item, Prop newProp);

	public delegate void OnLootedEvent(Ped ped);

	public const float InteractDistance = 1.5f;

	private readonly NativeMenu _mainMenu = new NativeMenu(string.Empty, "INVENTORY & RESOURCES");

	private readonly List<Ped> _lootedPeds = new List<Ped>();

	private Inventory _inventory;

	private readonly Keys _inventoryKey = (Keys)73;

	public static PlayerInventory Instance { get; private set; }

	private static Ped PlayerPed => Database.PlayerPed;

	private static Vector3 PlayerPosition => Database.PlayerPosition;

	public static event OnUsedFoodEvent FoodUsed;

	public static event OnUsedWeaponEvent WeaponUsed;

	public static event OnUsedBuildableEvent BuildableUsed;

	public static event OnLootedEvent LootedPed;

	public PlayerInventory()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Expected O, but got Unknown
		_inventoryKey = base.Settings.GetValue<Keys>("keys", "inventory_key", _inventoryKey);
		base.Settings.SetValue<Keys>("keys", "inventory_key", _inventoryKey);
		base.Settings.Save();
		Inventory inventory = Serializer.Deserialize<Inventory>(Config.InventoryFilePath);
		if (inventory == null)
		{
			inventory = new Inventory(MenuType.Player);
		}
		_inventory = inventory;
		_inventory.LoadMenus();
		Instance = this;
		MenuConrtoller.MenuPool.Add(_mainMenu);
		// Submenu entries for the inventory and resource lists (already pooled in LoadMenus).
		_mainMenu.AddSubMenu(_inventory.InventoryMenu, "Inventory");
		_mainMenu.AddSubMenu(_inventory.ResourceMenu, "Resources");

		NativeCheckboxItem editMode = new NativeCheckboxItem("Edit Mode", "Allow yourself to pickup objects.", true);
		editMode.CheckboxChanged += delegate
		{
			PlayerMap.Instance.EditMode = editMode.Checked;
		};
		NativeItem mainMenuItem = new NativeItem("Main Menu", "Navigate to the main menu. (For gamepad users)");
		mainMenuItem.Activated += delegate
		{
			_mainMenu.Visible = false;
			ModController.Instance.MainMenu.Visible = true;
		};
		NativeCheckboxItem devMode = new NativeCheckboxItem("Developer Mode", "Enable/Disable infinite items and resources.", _inventory.DeveloperMode);
		devMode.CheckboxChanged += delegate
		{
			if (_inventory == null)
			{
				return;
			}
			if (devMode.Checked)
			{
				string userInput = Game.GetUserInput(WindowTitle.EnterMessage60, "", 12);
				if (string.IsNullOrEmpty(userInput) || userInput.ToLower() != "michael")
				{
					devMode.Checked = false;
					Notifier.Show("Hint: Tamara Greenway's husband's first name.");
					return;
				}
			}
			_inventory.DeveloperMode = devMode.Checked;
			if (!devMode.Checked)
			{
				_inventory.Items.ForEach(delegate(InventoryItemBase i) { i.Amount = 0; });
				_inventory.Resources.ForEach(delegate(InventoryItemBase i) { i.Amount = 0; });
				_inventory.RefreshMenu();
			}
			else
			{
				Notifier.Show("Developer Mode: ~g~Activated~s~");
			}
			Serializer.Serialize(Config.InventoryFilePath, _inventory);
		};
		_mainMenu.Add(editMode);
		_mainMenu.Add(mainMenuItem);
		_mainMenu.Add(devMode);
		_inventory.ItemUsed += InventoryOnItemUsed;
		_inventory.AddedItem += delegate
		{
			Serializer.Serialize(Config.InventoryFilePath, _inventory);
		};
		base.Tick += OnTick;
		base.KeyUp += new KeyEventHandler(OnKeyUp);
		LootedPed += OnLootedPed;
	}

	private void OnLootedPed(Ped ped)
	{
		if (ped.IsHuman)
		{
			PickupLoot(ped);
		}
		else
		{
			AnimalLoot(ped);
		}
	}

	private void AnimalLoot(Ped ped)
	{
		if (!PlayerPed.Weapons.HasWeapon(WeaponHash.Knife))
		{
			Notifier.Show("You need a knife!");
		}
		else if (_inventory.AddItem(ItemFromName("Raw Meat"), 2, ItemType.Resource))
		{
			PlayerPed.Weapons.Select(WeaponHash.Knife, equipNow: true);
			Notifier.Show("You gutted the animal for ~g~raw meat~s~.");
			PlayerPed.Task.PlayAnimation("amb@world_human_gardener_plant@male@base", "base", 8f, 3000, AnimationFlags.None);
			_lootedPeds.Add(ped);
		}
	}

	public void PickupLoot(Ped ped, ItemType type = ItemType.Resource, int amountPerItemMin = 1, int amountPerItemMax = 3, float successChance = 0.2f)
	{
		List<InventoryItemBase> list = ((type == ItemType.Resource) ? _inventory.Resources : _inventory.Items);
		if (list.All((InventoryItemBase r) => r.Amount == r.MaxAmount))
		{
			Notifier.Show($"Your {type}s are full!");
			return;
		}
		int amount = 0;
		list.ForEach(delegate(InventoryItemBase i)
		{
			if (!(i.Id == "Cooked Meat"))
			{
				Random random = Database.Random;
				if (!(random.NextDouble() > (double)successChance))
				{
					int num = Database.Random.Next(amountPerItemMin, amountPerItemMax);
					if (i.Amount + num > i.MaxAmount)
					{
						num = i.MaxAmount - i.Amount;
					}
					_inventory.AddItem(i, num, type);
					amount += num;
				}
			}
		});
		Notifier.Show(string.Format("{0}", (amount > 0) ? $"{type}s: +~g~{amount}" : "Nothing found."), blinking: true);
		PlayerPed.Task.PlayAnimation("pickup_object", "pickup_low");
		if (!(ped == null))
		{
			_lootedPeds.Add(ped);
		}
	}

	private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (!MenuConrtoller.MenuPool.AreAnyVisible && keyEventArgs.KeyCode == _inventoryKey)
		{
			_mainMenu.Visible = !_mainMenu.Visible;
		}
	}

	private void InventoryOnItemUsed(InventoryItemBase item, ItemType type)
	{
		if (item == null || type == ItemType.Resource)
		{
			return;
		}
		if (item.GetType() == typeof(FoodInventoryItem))
		{
			FoodInventoryItem foodInventoryItem = (FoodInventoryItem)item;
			PlayerPed.Task.PlayAnimation(foodInventoryItem.AnimationDict, foodInventoryItem.AnimationName, 8f, foodInventoryItem.AnimationDuration, foodInventoryItem.AnimationFlags);
			PlayerInventory.FoodUsed?.Invoke(foodInventoryItem, foodInventoryItem.FoodType);
		}
		else if (item.GetType() == typeof(WeaponInventoryItem))
		{
			WeaponInventoryItem weaponInventoryItem = (WeaponInventoryItem)item;
			PlayerPed.Weapons.Give(weaponInventoryItem.Hash, weaponInventoryItem.Ammo, equipNow: true, isAmmoLoaded: true);
			PlayerInventory.WeaponUsed?.Invoke(weaponInventoryItem);
		}
		else if (item.GetType() == typeof(BuildableInventoryItem) || item.GetType() == typeof(WeaponStorageInventoryItem))
		{
			if (PlayerPed.IsInVehicle())
			{
				Notifier.Show("You can't build while in a vehicle!");
				return;
			}
			BuildableInventoryItem buildableInventoryItem = (BuildableInventoryItem)item;
			ItemPreview itemPreview = new ItemPreview();
			itemPreview.StartPreview(buildableInventoryItem.PropName, buildableInventoryItem.GroundOffset, buildableInventoryItem.IsDoor);
			// Interactive placement, but with a generous failsafe so an unexpected
			// exception in the preview can't stall this script's fiber forever.
			int safety = 0;
			while (!itemPreview.PreviewComplete && safety++ < 18000)
			{
				Script.Yield();
			}
			if (!itemPreview.PreviewComplete)
			{
				itemPreview.Abort();
				return;
			}
			Prop result = itemPreview.GetResult();
			if (result == null)
			{
				return;
			}
			AddBlipToProp(buildableInventoryItem, buildableInventoryItem.Id, result);
			PlayerInventory.BuildableUsed?.Invoke(buildableInventoryItem, result);
		}
		else if (item.GetType() == typeof(UsableInventoryItem))
		{
			UsableInventoryItem usableInventoryItem = (UsableInventoryItem)item;
			UsableItemEvent[] itemEvents = usableInventoryItem.ItemEvents;
			UsableItemEvent[] array = itemEvents;
			foreach (UsableItemEvent usableItemEvent in array)
			{
				switch (usableItemEvent.Event)
				{
				case ItemEvent.GiveArmor:
				{
					int num2 = (usableItemEvent.EventArgument as int?) ?? 0;
					PlayerPed.Armor += num2;
					break;
				}
				case ItemEvent.GiveHealth:
				{
					int num = (usableItemEvent.EventArgument as int?) ?? 0;
					PlayerPed.Health += num;
					break;
				}
				}
			}
		}
		else if (item.GetType() == typeof(CraftableInventoryItem))
		{
			CraftableInventoryItem craftableInventoryItem = (CraftableInventoryItem)item;
			if (!craftableInventoryItem.Validation())
			{
				return;
			}
		}
		_inventory.AddItem(item, -1, type);
	}

	private void OnTick(object sender, EventArgs eventArgs)
	{
		_inventory.ProcessKeys();
		GetWater();
		LootDeadPeds();
	}

	private void GetWater()
	{
		if (PlayerPed.IsInVehicle() || PlayerPed.IsSwimming || !PlayerPed.IsInWater || PlayerPed.IsPlayingAnim("pickup_object", "pickup_low"))
		{
			return;
		}
		InventoryItemBase inventoryItemBase = _inventory.Resources.Find((InventoryItemBase i) => i.Id == "Bottle");
		if (inventoryItemBase != null && inventoryItemBase.Amount > 0)
		{
			Game.DisableControlThisFrame(Control.Enter);
			if (Ctrl.DisabledJustPressed(Control.Enter))
			{
				PlayerPed.Task.PlayAnimation("pickup_object", "pickup_low");
				InventoryItemBase item = ItemFromName("Dirty Water");
				AddItem(item, 1, ItemType.Resource);
				AddItem(inventoryItemBase, -1, ItemType.Resource);
				Notifier.Show("Resources: -~r~1", blinking: true);
				Notifier.Show("Resources: +~g~1", blinking: true);
			}
		}
	}

	private void LootDeadPeds()
	{
		if (PlayerPed.IsInVehicle())
		{
			return;
		}
		Ped closest = World.GetClosest(PlayerPosition, World.GetNearbyPeds(PlayerPed, 1.5f));
		if (!(closest == null) && closest.IsDead && !_lootedPeds.Contains(closest))
		{
			UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to loot.");
			Game.DisableControlThisFrame(Control.Context);
			if (Ctrl.DisabledJustPressed(Control.Context))
			{
				PlayerInventory.LootedPed?.Invoke(closest);
			}
		}
	}

	private void Controller()
	{
	}

	public bool AddItem(InventoryItemBase item, int amount, ItemType type)
	{
		return item != null && _inventory.AddItem(item, amount, type);
	}

	public bool PickupItem(InventoryItemBase item, ItemType type)
	{
		return item != null && _inventory.AddItem(item, 1, type);
	}

	public InventoryItemBase ItemFromName(string id)
	{
		if (_inventory?.Items == null)
		{
			return null;
		}
		if (_inventory?.Resources == null)
		{
			return null;
		}
		InventoryItemBase[] array = _inventory.Items.Concat(_inventory.Resources).ToArray();
		return Array.Find(array, (InventoryItemBase i) => i.Id == id);
	}

	private static void AddBlipToProp(IProp item, string name, Entity entity)
	{
		if (item.BlipSprite != BlipSprite.Standard)
		{
			Blip blip = entity.AddBlip();
			blip.Sprite = item.BlipSprite;
			blip.Color = item.BlipColor;
			blip.Name = name;
		}
	}

	public bool HasItem(InventoryItemBase item, ItemType itemType)
	{
		if (item == null)
		{
			return false;
		}
		return itemType switch
		{
			ItemType.Item => _inventory.Items.Contains(item) && item.Amount > 0, 
			ItemType.Resource => _inventory.Resources.Contains(item) && item.Amount > 0, 
			_ => false, 
		};
	}
}
