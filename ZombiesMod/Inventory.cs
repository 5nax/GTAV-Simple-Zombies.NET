using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Menus;
using LemonUI.Scaleform;
using ZombiesMod.Static;

namespace ZombiesMod;

[Serializable]
public class Inventory
{
	public delegate void CraftedItemEvent(InventoryItemBase item);

	public delegate void ItemUsedEvent(InventoryItemBase item, ItemType type);

	public delegate void AddedItemEvent(InventoryItemBase item, int newAmount);

	public const float InteractDist = 2.5f;

	public List<InventoryItemBase> Items = new List<InventoryItemBase>();

	public List<InventoryItemBase> Resources = new List<InventoryItemBase>();

	[NonSerialized]
	public readonly MenuType MenuType;

	[NonSerialized]
	public NativeMenu InventoryMenu;

	[NonSerialized]
	public NativeMenu ResourceMenu;

	private static Vector3 PlayerPosition => Database.PlayerPosition;

	private static Ped PlayerPed => Database.PlayerPed;

	public bool DeveloperMode { get; set; }

	[field: NonSerialized]
	public event CraftedItemEvent TryCraft;

	[field: NonSerialized]
	public event ItemUsedEvent ItemUsed;

	[field: NonSerialized]
	public event AddedItemEvent AddedItem;

	public Inventory(MenuType menuType, bool ignoreContainers = false)
	{
		MenuType = menuType;
		InventoryItemBase inventoryItemBase = new InventoryItemBase(0, 20, "Alcohol", "A colorless volatile flammable liquid.");
		InventoryItemBase inventoryItemBase2 = new InventoryItemBase(0, 25, "Battery", "A resource that can provide an electrical charge.");
		InventoryItemBase inventoryItemBase3 = new InventoryItemBase(0, 25, "Binding", "A strong adhesive.");
		InventoryItemBase inventoryItemBase4 = new InventoryItemBase(0, 10, "Bottle", "A container used for storing drinks or other liquids..");
		InventoryItemBase inventoryItemBase5 = new InventoryItemBase(0, 25, "Cloth", "Woven or felted fabric.");
		InventoryItemBase inventoryItemBase6 = new InventoryItemBase(0, 25, "Dirty Water", "Liquid obtained from an undrinkable source of water.");
		InventoryItemBase inventoryItemBase7 = new InventoryItemBase(0, 25, "Metal", "It's freaking metal.");
		InventoryItemBase inventoryItemBase8 = new InventoryItemBase(0, 25, "Wood", "It's freaking wood.");
		InventoryItemBase inventoryItemBase9 = new InventoryItemBase(0, 25, "Plastic", "A synthetic material made from a wide range of organic polymers.");
		InventoryItemBase inventoryItemBase10 = new InventoryItemBase(0, 15, "Raw Meat", "Can be cooked to create ~g~Cooked Meat~s~.");
		InventoryItemBase inventoryItemBase11 = new InventoryItemBase(0, 10, "Matches", "Can be used to create fire.");
		InventoryItemBase inventoryItemBase12 = new InventoryItemBase(25, 25, "Weapon Parts", "Used to create weapon components, and weapons. (Weapons crafting coming soon)");
		InventoryItemBase inventoryItemBase13 = new InventoryItemBase(0, 25, "Vehicle Parts", "USed to repair vehicles.");
		UsableInventoryItem usableInventoryItem = new UsableInventoryItem(0, 10, "Bandage", "A strip of material used to bind a wound or to protect an injured part of the body.", new UsableItemEvent[2]
		{
			new UsableItemEvent(ItemEvent.GiveHealth, 25),
			new UsableItemEvent(ItemEvent.GiveArmor, 15)
		})
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase3, 1),
				new CraftableItemComponent(inventoryItemBase, 2),
				new CraftableItemComponent(inventoryItemBase5, 2)
			}
		};
		UsableInventoryItem antidote = new UsableInventoryItem(0, 5, "Antidote", "Cures the zombie infection. Craft from alcohol, binding and a battery.", new UsableItemEvent[1]
		{
			new UsableItemEvent(ItemEvent.CureInfection, 100)
		})
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase, 2),
				new CraftableItemComponent(inventoryItemBase3, 1),
				new CraftableItemComponent(inventoryItemBase2, 1)
			}
		};
		UsableInventoryItem fuelCan = new UsableInventoryItem(0, 5, "Fuel Can", "Refuels the vehicle you're driving. Craft from plastic, metal and alcohol.", new UsableItemEvent[1]
		{
			new UsableItemEvent(ItemEvent.Refuel, 50)
		})
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase9, 2),
				new CraftableItemComponent(inventoryItemBase7, 2),
				new CraftableItemComponent(inventoryItemBase, 1)
			}
		};
		CraftableInventoryItem craftableInventoryItem = new CraftableInventoryItem(0, 5, "Suppressor", "Can be used to suppress a rifle, pistol, shotgun, or SMG.", delegate
		{
			GTA.Weapon current = PlayerPed.Weapons.Current;
			if (current.Hash == WeaponHash.Unarmed)
			{
				Notifier.Show("No weapon selected!");
				return false;
			}
			WeaponComponentHash[] array = new WeaponComponentHash[5]
			{
				WeaponComponentHash.AtArSupp,
				WeaponComponentHash.AtArSupp02,
				WeaponComponentHash.AtPiSupp,
				WeaponComponentHash.AtPiSupp02,
				WeaponComponentHash.AtSrSupp
			};
			WeaponComponentHash[] array2 = array;
			foreach (WeaponComponentHash weaponComponent in array2)
			{
				if (Function.Call<bool>((Hash)0x5CEE3DF569CECAB0uL, new InputArgument[2]
				{
					(uint)current.Hash,
					(uint)weaponComponent
				}) && !current.Components[weaponComponent].Active)
				{
					current.Components[weaponComponent].Active = true;
					return true;
				}
			}
			Notifier.Show("You can't equip this right now.");
			return false;
		})
		{
			RequiredComponents = new CraftableItemComponent[2]
			{
				new CraftableItemComponent(inventoryItemBase12, 2),
				new CraftableItemComponent(inventoryItemBase3, 1)
			}
		};
		BuildableInventoryItem buildableInventoryItem = new BuildableInventoryItem(0, 5, "Sand Block", "Used to provide cover in combat situations", "prop_mb_sandblock_02", BlipSprite.Standard, BlipColor.White, Vector3.Zero, interactable: false, isDoor: false, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[4]
			{
				new CraftableItemComponent(inventoryItemBase7, 3),
				new CraftableItemComponent(inventoryItemBase3, 2),
				new CraftableItemComponent(inventoryItemBase5, 1),
				new CraftableItemComponent(inventoryItemBase8, 2)
			}
		};
		BuildableInventoryItem buildableInventoryItem2 = new BuildableInventoryItem(0, 2, "Work Bench", "Can be used to craft ammunition.", "prop_tool_bench02", BlipSprite.AmmuNation, BlipColor.Yellow, Vector3.Zero, interactable: true, isDoor: false, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[4]
			{
				new CraftableItemComponent(inventoryItemBase7, 15),
				new CraftableItemComponent(inventoryItemBase8, 5),
				new CraftableItemComponent(inventoryItemBase9, 5),
				new CraftableItemComponent(inventoryItemBase3, 5)
			}
		};
		BuildableInventoryItem buildableInventoryItem3 = new BuildableInventoryItem(0, 3, "Gate", "A metal gate that can be opened by vehicles or peds.", "prop_gate_prison_01", BlipSprite.Standard, BlipColor.White, Vector3.Zero, interactable: true, isDoor: true, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase7, 5),
				new CraftableItemComponent(inventoryItemBase3, 3),
				new CraftableItemComponent(inventoryItemBase2, 1)
			}
		};
		WeaponInventoryItem weaponInventoryItem = new WeaponInventoryItem(0, 25, "Molotov", "A bottle-based improvised incendiary weapon.", 1, WeaponHash.Molotov, null)
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase, 1),
				new CraftableItemComponent(inventoryItemBase5, 1),
				new CraftableItemComponent(inventoryItemBase4, 1)
			}
		};
		WeaponInventoryItem weaponInventoryItem2 = new WeaponInventoryItem(0, 1, "Knife", "An improvised knife.", 1, WeaponHash.Knife, null)
		{
			RequiredComponents = new CraftableItemComponent[2]
			{
				new CraftableItemComponent(inventoryItemBase7, 3),
				new CraftableItemComponent(inventoryItemBase3, 1)
			}
		};
		WeaponInventoryItem weaponInventoryItem3 = new WeaponInventoryItem(0, 5, "Flashlight", "A battery-operated portable light.", 1, WeaponHash.Flashlight, null)
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase2, 4),
				new CraftableItemComponent(inventoryItemBase9, 4),
				new CraftableItemComponent(inventoryItemBase3, 4)
			}
		};
		FoodInventoryItem foodInventoryItem = new FoodInventoryItem(0, 15, "Cooked Meat", "Can be creating from cooking raw meat.", "mp_player_inteat@burger", "mp_player_int_eat_burger", AnimationFlags.UpperBodyOnly, -1, FoodType.Food, 0.25f)
		{
			RequiredComponents = new CraftableItemComponent[2]
			{
				new CraftableItemComponent(inventoryItemBase10, 1),
				new CraftableItemComponent(inventoryItemBase, 2)
			},
			NearbyResource = NearbyResource.CampFire
		};
		FoodInventoryItem foodInventoryItem2 = new FoodInventoryItem(0, 15, "Packaged Food", "Usually obtained from stores around Los Santos.", "mp_player_inteat@pnq", "loop", AnimationFlags.UpperBodyOnly, -1, FoodType.SpecialFood, 0.3f);
		FoodInventoryItem foodInventoryItem3 = new FoodInventoryItem(0, 15, "Clean Water", "Can be made from dirty water or obtained from stores around Los Santos.", "mp_player_intdrink", "loop_bottle", AnimationFlags.UpperBodyOnly, -1, FoodType.Water, 0.35f)
		{
			RequiredComponents = new CraftableItemComponent[1]
			{
				new CraftableItemComponent(inventoryItemBase6, 1)
			},
			NearbyResource = NearbyResource.CampFire
		};
		BuildableInventoryItem buildableInventoryItem4 = new BuildableInventoryItem(1, 5, "Tent", "A portable shelter made of cloth, supported by one or more poles and stretched tight by cords or loops attached to pegs driven into the ground.", "prop_skid_tent_01", BlipSprite.CaptureHouse, BlipColor.White, Vector3.Zero, interactable: true, isDoor: false, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase8, 3),
				new CraftableItemComponent(inventoryItemBase5, 4),
				new CraftableItemComponent(inventoryItemBase3, 3)
			}
		};
		BuildableInventoryItem buildableInventoryItem5 = new BuildableInventoryItem(1, 5, "Camp Fire", "An open-air fire in a camp, used for cooking and as a focal point for social activity.", "prop_beach_fire", BlipSprite.Standard, BlipColor.White, Vector3.Zero, interactable: false, isDoor: false, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase8, 3),
				new CraftableItemComponent(inventoryItemBase, 1),
				new CraftableItemComponent(inventoryItemBase11, 1)
			}
		};
		BuildableInventoryItem buildableInventoryItem6 = new BuildableInventoryItem(0, 15, "Wall", "A wooden wall that can be used for creating shelters.", "prop_fncconstruc_01d", BlipSprite.Standard, BlipColor.White, Vector3.Zero, interactable: false, isDoor: false, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[2]
			{
				new CraftableItemComponent(inventoryItemBase8, 4),
				new CraftableItemComponent(inventoryItemBase3, 3)
			}
		};
		BuildableInventoryItem buildableInventoryItem7 = new BuildableInventoryItem(0, 10, "Barrier", "A wooden barrier that can be used to barricade gaps in your safe zone.", "prop_fncwood_16b", BlipSprite.Standard, BlipColor.White, Vector3.Zero, interactable: false, isDoor: false, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[2]
			{
				new CraftableItemComponent(inventoryItemBase8, 5),
				new CraftableItemComponent(inventoryItemBase3, 2)
			}
		};
		BuildableInventoryItem buildableInventoryItem8 = new BuildableInventoryItem(0, 5, "Door", "A  hinged, sliding, or revolving barrier at the entrance to a building, room, or vehicle, or in the framework of a cupboard.", "ex_p_mp_door_office_door01", BlipSprite.Standard, BlipColor.White, Vector3.Zero, interactable: true, isDoor: true, canBePickedUp: true)
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase8, 3),
				new CraftableItemComponent(inventoryItemBase3, 1),
				new CraftableItemComponent(inventoryItemBase7, 1)
			}
		};
		CraftableInventoryItem craftableInventoryItem2 = new CraftableInventoryItem(0, 10, "Vehicle Repair Kit", "Used to repair vehicle engines.", delegate
		{
			Notifier.Show("You can only use this when repairing a vehicle.");
			return false;
		})
		{
			RequiredComponents = new CraftableItemComponent[3]
			{
				new CraftableItemComponent(inventoryItemBase13, 5),
				new CraftableItemComponent(inventoryItemBase7, 5),
				new CraftableItemComponent(inventoryItemBase3, 2)
			}
		};
		Items.AddRange(new InventoryItemBase[19]
		{
			usableInventoryItem, antidote, fuelCan, weaponInventoryItem, weaponInventoryItem2, weaponInventoryItem3, foodInventoryItem, foodInventoryItem2, foodInventoryItem3, buildableInventoryItem4,
			buildableInventoryItem5, buildableInventoryItem6, buildableInventoryItem7, buildableInventoryItem8, craftableInventoryItem, buildableInventoryItem3, buildableInventoryItem, buildableInventoryItem2, craftableInventoryItem2
		});
		Resources.AddRange(new InventoryItemBase[13]
		{
			inventoryItemBase3, inventoryItemBase, inventoryItemBase5, inventoryItemBase4, inventoryItemBase7, inventoryItemBase8, inventoryItemBase2, inventoryItemBase9, inventoryItemBase10, inventoryItemBase6,
			inventoryItemBase11, inventoryItemBase12, inventoryItemBase13
		});
		Items.Sort((InventoryItemBase c1, InventoryItemBase c2) => string.Compare(c1.Id, c2.Id, StringComparison.Ordinal));
		Resources.Sort((InventoryItemBase c1, InventoryItemBase c2) => string.Compare(c1.Id, c2.Id, StringComparison.Ordinal));
		LoadMenus();
		if (!ignoreContainers)
		{
			WeaponStorageInventoryItem item = new WeaponStorageInventoryItem(0, 1, "Weapons Crate", "A crate specifically used to store weapons.", "hei_prop_carrier_crate_01a", BlipSprite.AssaultRifle, BlipColor.White, Vector3.Zero, interactable: true, isDoor: false, canBePickedUp: true, new List<Weapon>())
			{
				RequiredComponents = new CraftableItemComponent[4]
				{
					new CraftableItemComponent(inventoryItemBase7, 15),
					new CraftableItemComponent(inventoryItemBase8, 15),
					new CraftableItemComponent(inventoryItemBase9, 15),
					new CraftableItemComponent(inventoryItemBase3, 10)
				}
			};
			Items.Add(item);
		}
	}

	public void LoadMenus()
	{
		InventoryMenu = new NativeMenu("Inventory", "SELECT AN ITEM");
		ResourceMenu = new NativeMenu("Resources", "SELECT AN ITEM");
		AddItemsToMenu(InventoryMenu, Items, ItemType.Item);
		AddItemsToMenu(ResourceMenu, Resources, ItemType.Resource);
		MenuConrtoller.MenuPool.Add(InventoryMenu);
		MenuConrtoller.MenuPool.Add(ResourceMenu);
		RefreshMenu();
		if (MenuType == MenuType.Player)
		{
			InventoryMenu.Buttons.Add(new InstructionalButton("Blueprint", Control.Enter));
			InventoryMenu.Buttons.Add(new InstructionalButton("Craft", Control.LookBehind));
		}
	}

	public void RefreshMenu()
	{
		UpdateMenuSpecific(InventoryMenu, Items, MenuType == MenuType.Player);
		UpdateMenuSpecific(ResourceMenu, Resources, leftBadges: false);
	}

	public void ProcessKeys()
	{
		if (MenuType != MenuType.Player || !InventoryMenu.Visible)
		{
			return;
		}
		Game.DisableControlThisFrame(Control.Enter);
		Game.DisableControlThisFrame(Control.VehicleExit);
		Game.DisableControlThisFrame(Control.LookBehind);
		if (Ctrl.DisabledJustPressed(Control.Enter))
		{
			ICraftable selectedInventoryItem = GetSelectedInventoryItem<ICraftable>();
			if (selectedInventoryItem == null)
			{
				return;
			}
			StringBuilder str = new StringBuilder("Blueprint:\n");
			if (selectedInventoryItem.RequiredComponents != null && (selectedInventoryItem.RequiredComponents.Any() || DeveloperMode))
			{
				Array.ForEach(selectedInventoryItem.RequiredComponents, delegate(CraftableItemComponent i)
				{
					str.Append(string.Format("{0}{1}~s~ / {2} {3}\n", (i.Resource.Amount >= i.RequiredAmount) ? "~g~" : "~r~", i.Resource.Amount, i.RequiredAmount, i.Resource.Id));
				});
				Notifier.Show(str.ToString());
			}
		}
		else if (Ctrl.DisabledJustPressed(Control.LookBehind))
		{
			InventoryItemBase selectedInventoryItem2 = GetSelectedInventoryItem<InventoryItemBase>();
			ICraftable craftable = selectedInventoryItem2 as ICraftable;
			if (selectedInventoryItem2 == null)
			{
				throw new NullReferenceException("item");
			}
			if (craftable != null)
			{
				Craft(selectedInventoryItem2, craftable);
			}
		}
	}

	public bool AddItem(InventoryItemBase item, int amount, ItemType type)
	{
		if (!DeveloperMode)
		{
			if (item.Amount + amount < 0)
			{
				return false;
			}
			if (item.Amount + amount > item.MaxAmount)
			{
				Notifier.Show($"There's not enough room for anymore ~g~{item.Id}s~s~.", blinking: true);
				return false;
			}
		}
		item.Amount += amount;
		switch (type)
		{
		case ItemType.Item:
			UpdateMenuSpecific(InventoryMenu, Items, MenuType == MenuType.Player);
			break;
		case ItemType.Resource:
			UpdateMenuSpecific(ResourceMenu, Resources, leftBadges: false);
			break;
		}
		RefreshMenu();
		this.AddedItem?.Invoke(item, item.Amount);
		return true;
	}

	private void Craft(InventoryItemBase item, ICraftable craftable)
	{
		if (MenuType != MenuType.Player || item == null || craftable == null || (!DeveloperMode && (!CanCraftItem(craftable) || item.Amount >= item.MaxAmount)))
		{
			return;
		}
		FoodInventoryItem foodInventoryItem = item as FoodInventoryItem;
		if (!DeveloperMode)
		{
			NearbyResource? nearbyResource = foodInventoryItem?.NearbyResource;
			NearbyResource? nearbyResource2 = nearbyResource;
			NearbyResource? nearbyResource3 = nearbyResource2;
			if (nearbyResource3.HasValue)
			{
				NearbyResource valueOrDefault = nearbyResource3.GetValueOrDefault();
				if (valueOrDefault == NearbyResource.CampFire)
				{
					Prop[] nearbyProps = World.GetNearbyProps(PlayerPosition, 2.5f, "prop_beach_fire");
					if (!nearbyProps.Any())
					{
						Notifier.Show("There's no ~g~Camp Fire~s~ nearby.");
						return;
					}
				}
			}
		}
		AddItem(item, 1, ItemType.Item);
		Array.ForEach(craftable.RequiredComponents, delegate(CraftableItemComponent c)
		{
			AddItem(c.Resource, -c.RequiredAmount, ItemType.Resource);
		});
		this.TryCraft?.Invoke(item);
	}

	private void AddItemsToMenu(NativeMenu menu, List<InventoryItemBase> items, ItemType type)
	{
		items.ForEach(delegate(InventoryItemBase i)
		{
			i.CreateMenuItem();
			menu.Add(i.MenuItem);
			i.MenuItem.Activated += delegate
			{
				if (i is WeaponInventoryItem { Amount: >0 } weaponInventoryItem && PlayerPed.Weapons.HasWeapon(weaponInventoryItem.Hash))
				{
					Notifier.Show("You already have that weapon!");
				}
				else if (i.Amount > 0)
				{
					this.ItemUsed?.Invoke(i, type);
				}
			};
		});
	}

	private void UpdateMenuSpecific(NativeMenu menu, List<InventoryItemBase> collection, bool leftBadges)
	{
		menu.Items.ForEach(delegate(NativeItem menuItem)
		{
			InventoryItemBase itemFromMenuItem = GetItemFromMenuItem<InventoryItemBase>(collection, menuItem);
			if (itemFromMenuItem != null)
			{
				// Right-aligned amount (LemonUI uses AltTitle for the right label).
				// Craftable-state indicator: a checkmark prefix when craftable.
				string craftMark = (leftBadges && (DeveloperMode || CanCraftItem(itemFromMenuItem as ICraftable))) ? "~g~✓~s~ " : string.Empty;
				menuItem.AltTitle = $"{craftMark}{itemFromMenuItem.Amount}/{itemFromMenuItem.MaxAmount}";
			}
		});
	}

	private bool CanCraftItem(ICraftable craftable)
	{
		if (craftable?.RequiredComponents == null)
		{
			return false;
		}
		CraftableItemComponent[] requiredComponents = craftable.RequiredComponents;
		foreach (CraftableItemComponent craftableItemComponent in requiredComponents)
		{
			InventoryItemBase resource = craftableItemComponent.Resource;
			if (Resources.Contains(resource) && Resources.Find((InventoryItemBase inventoryItemBase) => resource == inventoryItemBase)?.Amount < craftableItemComponent.RequiredAmount)
			{
				return false;
			}
		}
		return true;
	}

	private T GetSelectedInventoryItem<T>() where T : class
	{
		if (InventoryMenu.Items.Count == 0 || InventoryMenu.SelectedIndex < 0)
		{
			return null;
		}
		NativeItem menuItem = InventoryMenu.Items[InventoryMenu.SelectedIndex];
		return GetItemFromMenuItem<T>(Items, menuItem);
	}

	private static T GetItemFromMenuItem<T>(List<InventoryItemBase> collection, NativeItem menuItem) where T : class
	{
		return collection.Find((InventoryItemBase i) => i.MenuItem == menuItem) as T;
	}
}
