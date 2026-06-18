using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using ZombiesMod.Extensions;
using ZombiesMod.PlayerManagement;
using ZombiesMod.Static;
using ZombiesMod.Wrappers;

namespace ZombiesMod.Scripts;

public class MapInteraction : Script
{
	private const int AmmoPerPart = 10;

	private readonly float _enemyRangeForSleeping = 50f;

	private readonly int _sleepHours = 8;

	private readonly UIMenu _weaponsMenu = new UIMenu("Weapon Crate", "SELECT AN OPTION");

	private readonly UIMenu _storageMenu;

	private readonly UIMenu _myWeaponsMenu;

	private readonly UIMenu _craftWeaponsMenu = new UIMenu("Work Bench", "SELECT AN OPTION");

	private readonly Dictionary<WeaponGroup, int> _requiredAmountDictionary;

	private static Ped PlayerPed => Database.PlayerPed;

	private static Player Player => Database.Player;

	public MapInteraction()
	{
		PlayerMap.Interacted += MapOnInteracted;
		MenuConrtoller.MenuPool.Add(_weaponsMenu);
		MenuConrtoller.MenuPool.Add(_craftWeaponsMenu);
		_storageMenu = MenuConrtoller.MenuPool.AddSubMenu(_weaponsMenu, "Storage");
		_myWeaponsMenu = MenuConrtoller.MenuPool.AddSubMenu(_weaponsMenu, "Give");
		_enemyRangeForSleeping = base.Settings.GetValue("map_interaction", "enemy_range_for_sleeping", _enemyRangeForSleeping);
		_sleepHours = base.Settings.GetValue("map_interaction", "sleep_hours", _sleepHours);
		base.Settings.SetValue("map_interaction", "enemy_range_for_sleeping", _enemyRangeForSleeping);
		base.Settings.SetValue("map_interaction", "sleep_hours", _sleepHours);
		_requiredAmountDictionary = new Dictionary<WeaponGroup, int>
		{
			{
				WeaponGroup.Sniper,
				2
			},
			{
				WeaponGroup.Heavy,
				5
			},
			{
				WeaponGroup.MG,
				3
			},
			{
				WeaponGroup.PetrolCan,
				1
			}
		};
		base.Aborted += OnAborted;
	}

	private static void OnAborted(object sender, EventArgs eventArgs)
	{
		PlayerPed.IsVisible = true;
		PlayerPed.FreezePosition = false;
		Player.CanControlCharacter = true;
		if (!PlayerPed.IsDead)
		{
			Game.FadeScreenIn(0);
		}
	}

	private void MapOnInteracted(MapProp mapProp, InventoryItemBase inventoryItem)
	{
		if (inventoryItem is BuildableInventoryItem buildableInventoryItem)
		{
			switch (buildableInventoryItem.Id)
			{
			case "Tent":
				Sleep(mapProp.Position);
				break;
			case "Weapons Crate":
				UseWeaponsCrate(mapProp);
				break;
			case "Work Bench":
				CraftAmmo();
				break;
			}
			if (buildableInventoryItem.IsDoor)
			{
				Prop prop = new Prop(mapProp.Handle);
				prop.SetStateOfDoor(!prop.GetDoorLockState(), DoorState.Closed);
			}
		}
	}

	private void CraftAmmo()
	{
		_craftWeaponsMenu.Clear();
		WeaponGroup[] source = (WeaponGroup[])Enum.GetValues(typeof(WeaponGroup));
		source = source.Where((WeaponGroup w) => w != WeaponGroup.PetrolCan && w != WeaponGroup.Unarmed && w != WeaponGroup.Melee && w != (WeaponGroup)3352383570u).ToArray();
		List<WeaponGroup> list = source.ToList();
		list.Add(WeaponGroup.AssaultRifle);
		source = list.ToArray();
		WeaponGroup[] array = source;
		for (int num = 0; num < array.Length; num++)
		{
			WeaponGroup weaponGroup = array[num];
			UIMenuItem uIMenuItem = new UIMenuItem(string.Format("{0}", (weaponGroup == WeaponGroup.AssaultRifle) ? "Assult Rifle" : weaponGroup.ToString()), $"Craft ammo for {weaponGroup}");
			uIMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Ammo);
			int required = GetRequiredPartsForWeaponGroup(weaponGroup);
			uIMenuItem.Description = $"Required Weapon Parts: ~y~{required}~s~";
			_craftWeaponsMenu.AddItem(uIMenuItem);
			uIMenuItem.Activated += delegate
			{
				InventoryItemBase inventoryItemBase = PlayerInventory.Instance.ItemFromName("Weapon Parts");
				if (inventoryItemBase != null)
				{
					if (inventoryItemBase.Amount >= required)
					{
						WeaponHash[] array2 = (WeaponHash[])Enum.GetValues(typeof(WeaponHash));
						WeaponHash hash = Array.Find(array2, (WeaponHash h) => PlayerPed.Weapons.HasWeapon(h) && PlayerPed.Weapons[h].Group == weaponGroup);
						GTA.Weapon weapon = PlayerPed.Weapons[hash];
						if (weapon != null)
						{
							int num2 = 10 * required;
							if (weapon.Ammo + num2 <= weapon.MaxAmmo)
							{
								PlayerPed.Weapons.Select(weapon);
								if (weapon.Ammo + num2 > weapon.MaxAmmo)
								{
									weapon.Ammo = weapon.MaxAmmo;
								}
								else
								{
									weapon.Ammo += num2;
								}
								PlayerInventory.Instance.AddItem(inventoryItemBase, -required, ItemType.Resource);
							}
						}
					}
					else
					{
						UI.Notify("Not enough weapon parts.");
					}
				}
			};
		}
		_craftWeaponsMenu.Visible = !_craftWeaponsMenu.Visible;
	}

	private int GetRequiredPartsForWeaponGroup(WeaponGroup group)
	{
		return (!_requiredAmountDictionary.ContainsKey(group)) ? 1 : _requiredAmountDictionary[group];
	}

	private void UseWeaponsCrate(MapProp prop)
	{
		if (prop?.Weapons == null)
		{
			return;
		}
		_weaponsMenu.OnMenuChange += delegate(UIMenu oldMenu, UIMenu newMenu, bool forward)
		{
			if (newMenu == _storageMenu)
			{
				TradeOffWeapons(prop, prop.Weapons, _storageMenu, giveToPlayer: true);
			}
			else if (newMenu == _myWeaponsMenu)
			{
				List<Weapon> playerWeapons = new List<Weapon>();
				WeaponHash[] source = (WeaponHash[])Enum.GetValues(typeof(WeaponHash));
				WeaponComponent[] weaponComponents = (WeaponComponent[])Enum.GetValues(typeof(WeaponComponent));
				source.ToList().ForEach(delegate(WeaponHash hash)
				{
					if (hash != WeaponHash.Unarmed && PlayerPed.Weapons.HasWeapon(hash))
					{
						GTA.Weapon weapon = PlayerPed.Weapons[hash];
						WeaponComponent[] components = weaponComponents.Where((WeaponComponent c) => PlayerPed.Weapons[hash].IsComponentActive(c)).ToArray();
						Weapon item = new Weapon(weapon.Ammo, hash, components);
						playerWeapons.Add(item);
					}
				});
				TradeOffWeapons(prop, playerWeapons, _myWeaponsMenu, giveToPlayer: false);
			}
		};
		_weaponsMenu.Visible = !_weaponsMenu.Visible;
	}

	private static void TradeOffWeapons(MapProp item, List<Weapon> weapons, UIMenu currentMenu, bool giveToPlayer)
	{
		UIMenuItem uIMenuItem = new UIMenuItem("Back");
		uIMenuItem.Activated += delegate(UIMenu sender, UIMenuItem selectedItem)
		{
			sender.GoBack();
		};
		currentMenu.Clear();
		currentMenu.AddItem(uIMenuItem);
		Action notify = delegate
		{
			PlayerMap.Instance.NotifyListChanged();
		};
		weapons.ForEach(delegate(Weapon weapon)
		{
			UIMenuItem uIMenuItem2 = new UIMenuItem($"{weapon.Hash}");
			currentMenu.AddItem(uIMenuItem2);
			uIMenuItem2.Activated += delegate
			{
				currentMenu.RemoveItemAt(currentMenu.CurrentSelection);
				currentMenu.RefreshIndex();
				if (giveToPlayer)
				{
					PlayerPed.Weapons.Give(weapon.Hash, 0, equipNow: true, isAmmoLoaded: true);
					PlayerPed.Weapons[weapon.Hash].Ammo = weapon.Ammo;
					weapon.Components.ToList().ForEach(delegate(WeaponComponent component)
					{
						PlayerPed.Weapons[weapon.Hash].SetComponent(component, on: true);
					});
					item.Weapons.Remove(weapon);
					notify();
				}
				else
				{
					PlayerPed.Weapons.Remove(weapon.Hash);
					item.Weapons.Add(weapon);
					notify();
				}
			};
		});
		currentMenu.RefreshIndex();
	}

	private void Sleep(Vector3 position)
	{
		Ped[] array = World.GetNearbyPeds(position, _enemyRangeForSleeping).Where(IsEnemy).ToArray();
		if (!array.Any())
		{
			TimeSpan currentDayTime = World.CurrentDayTime + new TimeSpan(0, _sleepHours, 0, 0);
			PlayerPed.IsVisible = false;
			Player.CanControlCharacter = false;
			PlayerPed.FreezePosition = true;
			Game.FadeScreenOut(2000);
			Script.Wait(2000);
			World.CurrentDayTime = currentDayTime;
			PlayerPed.IsVisible = true;
			Player.CanControlCharacter = true;
			PlayerPed.FreezePosition = false;
			PlayerPed.ClearBloodDamage();
			Weather[] source = (Weather[])Enum.GetValues(typeof(Weather));
			source = source.Where((Weather w) => w != Weather.Blizzard && w != Weather.Christmas && w != Weather.Snowing && w != Weather.Snowlight && w != Weather.Unknown).ToArray();
			Weather weather = source[Database.Random.Next(source.Length)];
			World.Weather = weather;
			Script.Wait(2000);
			Game.FadeScreenIn(2000);
		}
		else
		{
			UI.Notify("There are ~r~enemies~s~ nearby.");
			UI.Notify("Marking them on your map.");
			Array.ForEach(array, AddBlip);
		}
	}

	private static void AddBlip(Ped ped)
	{
		if (!ped.CurrentBlip.Exists())
		{
			Blip blip = ped.AddBlip();
			blip.Name = "Enemy Ped";
			EntityEventWrapper entityEventWrapper = new EntityEventWrapper(ped);
			entityEventWrapper.Died += delegate(EntityEventWrapper sender, Entity entity)
			{
				entity.CurrentBlip?.Remove();
				sender.Dispose();
			};
			entityEventWrapper.Aborted += delegate(EntityEventWrapper sender, Entity entity)
			{
				entity.CurrentBlip?.Remove();
			};
		}
	}

	private static bool IsEnemy(Ped ped)
	{
		return (ped.IsHuman && !ped.IsDead && ped.GetRelationshipWithPed(PlayerPed) == Relationship.Hate) || ped.IsInCombatAgainst(PlayerPed);
	}
}
