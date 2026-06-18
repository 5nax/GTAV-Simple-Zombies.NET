using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Menus;
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

	private readonly NativeMenu _weaponsMenu = new NativeMenu("Weapon Crate", "SELECT AN OPTION");

	private readonly NativeMenu _storageMenu = new NativeMenu("Weapon Crate", "Storage");

	private readonly NativeMenu _myWeaponsMenu = new NativeMenu("Weapon Crate", "Give");

	private readonly NativeMenu _craftWeaponsMenu = new NativeMenu("Work Bench", "SELECT AN OPTION");

	private readonly Dictionary<WeaponGroup, int> _requiredAmountDictionary;

	// The crate currently being interacted with; submenus populate from this on open.
	private MapProp _currentCrate;

	private static Ped PlayerPed => Database.PlayerPed;

	private static Player Player => Database.Player;

	public MapInteraction()
	{
		PlayerMap.Interacted += MapOnInteracted;
		MenuConrtoller.MenuPool.Add(_weaponsMenu);
		MenuConrtoller.MenuPool.Add(_craftWeaponsMenu);
		MenuConrtoller.MenuPool.Add(_storageMenu);
		MenuConrtoller.MenuPool.Add(_myWeaponsMenu);
		_weaponsMenu.AddSubMenu(_storageMenu, "Storage");
		_weaponsMenu.AddSubMenu(_myWeaponsMenu, "Give");

		// Populate the trade submenus on open. Subscribed ONCE here (the original
		// re-added an OnMenuChange handler on every crate use, leaking delegates).
		_storageMenu.Shown += delegate
		{
			if (_currentCrate?.Weapons != null)
			{
				PopulateTradeMenu(_currentCrate, _currentCrate.Weapons, _storageMenu, giveToPlayer: true);
			}
		};
		_myWeaponsMenu.Shown += delegate
		{
			PopulateTradeMenu(_currentCrate, GetPlayerWeapons(), _myWeaponsMenu, giveToPlayer: false);
		};

		_enemyRangeForSleeping = Settings.GetValue("map_interaction", "enemy_range_for_sleeping", _enemyRangeForSleeping);
		_sleepHours = Settings.GetValue("map_interaction", "sleep_hours", _sleepHours);
		Settings.SetValue("map_interaction", "enemy_range_for_sleeping", _enemyRangeForSleeping);
		Settings.SetValue("map_interaction", "sleep_hours", _sleepHours);
		_requiredAmountDictionary = new Dictionary<WeaponGroup, int>
		{
			{ WeaponGroup.Sniper, 2 },
			{ WeaponGroup.Heavy, 5 },
			{ WeaponGroup.MG, 3 },
			{ WeaponGroup.PetrolCan, 1 }
		};
		Aborted += OnAborted;
	}

	private static void OnAborted(object sender, EventArgs eventArgs)
	{
		PlayerPed.IsVisible = true;
		PlayerPed.IsPositionFrozen = false;
		Player.CanControlCharacter = true;
		if (!PlayerPed.IsDead)
		{
			GTA.UI.Screen.FadeIn(0);
		}
	}

	private void MapOnInteracted(MapProp mapProp, InventoryItemBase inventoryItem)
	{
		if (!(inventoryItem is BuildableInventoryItem buildableInventoryItem))
		{
			return;
		}
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
			Prop prop = (Prop)GTA.Entity.FromHandle(mapProp.Handle);
			if (prop.Exists())
			{
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
		foreach (WeaponGroup weaponGroup in list)
		{
			WeaponGroup group = weaponGroup;
			int required = GetRequiredPartsForWeaponGroup(group);
			NativeItem item = new NativeItem(
				(group == WeaponGroup.AssaultRifle) ? "Assault Rifle" : group.ToString(),
				$"Required Weapon Parts: ~y~{required}~s~");
			_craftWeaponsMenu.Add(item);
			item.Activated += delegate
			{
				InventoryItemBase parts = PlayerInventory.Instance.ItemFromName("Weapon Parts");
				if (parts == null)
				{
					return;
				}
				if (parts.Amount < required)
				{
					Notifier.Show("Not enough weapon parts.");
					return;
				}
				WeaponHash[] all = (WeaponHash[])Enum.GetValues(typeof(WeaponHash));
				WeaponHash hash = Array.Find(all, (WeaponHash h) => PlayerPed.Weapons.HasWeapon(h) && PlayerPed.Weapons[h].Group == group);
				GTA.Weapon weapon = PlayerPed.Weapons[hash];
				if (weapon == null)
				{
					return;
				}
				int ammo = AmmoPerPart * required;
				if (weapon.Ammo + ammo <= weapon.MaxAmmo)
				{
					PlayerPed.Weapons.Select(weapon);
					weapon.Ammo = Math.Min(weapon.Ammo + ammo, weapon.MaxAmmo);
					PlayerInventory.Instance.AddItem(parts, -required, ItemType.Resource);
				}
			};
		}
		_craftWeaponsMenu.Visible = !_craftWeaponsMenu.Visible;
	}

	private int GetRequiredPartsForWeaponGroup(WeaponGroup group)
	{
		return _requiredAmountDictionary.ContainsKey(group) ? _requiredAmountDictionary[group] : 1;
	}

	private void UseWeaponsCrate(MapProp prop)
	{
		if (prop?.Weapons == null)
		{
			return;
		}
		_currentCrate = prop;
		_weaponsMenu.Visible = !_weaponsMenu.Visible;
	}

	private static List<Weapon> GetPlayerWeapons()
	{
		List<Weapon> playerWeapons = new List<Weapon>();
		WeaponComponentHash[] allComponents = (WeaponComponentHash[])Enum.GetValues(typeof(WeaponComponentHash));
		foreach (WeaponHash hash in (WeaponHash[])Enum.GetValues(typeof(WeaponHash)))
		{
			if (hash != WeaponHash.Unarmed && PlayerPed.Weapons.HasWeapon(hash))
			{
				GTA.Weapon weapon = PlayerPed.Weapons[hash];
				WeaponComponentHash[] components = allComponents.Where((WeaponComponentHash c) => weapon.Components[c].Active).ToArray();
				playerWeapons.Add(new Weapon(weapon.Ammo, hash, components));
			}
		}
		return playerWeapons;
	}

	private static void PopulateTradeMenu(MapProp crate, List<Weapon> weapons, NativeMenu menu, bool giveToPlayer)
	{
		if (crate == null)
		{
			return;
		}
		menu.Clear();
		foreach (Weapon weapon in weapons.ToList())
		{
			Weapon w = weapon;
			NativeItem item = new NativeItem(w.Hash.ToString());
			menu.Add(item);
			item.Activated += delegate
			{
				menu.Remove(item);
				if (giveToPlayer)
				{
					PlayerPed.Weapons.Give(w.Hash, 0, equipNow: true, isAmmoLoaded: true);
					PlayerPed.Weapons[w.Hash].Ammo = w.Ammo;
					if (w.Components != null)
					{
						foreach (WeaponComponentHash component in w.Components)
						{
							PlayerPed.Weapons[w.Hash].Components[component].Active = true;
						}
					}
					crate.Weapons.Remove(w);
				}
				else
				{
					PlayerPed.Weapons.Remove(w.Hash);
					crate.Weapons.Add(w);
				}
				PlayerMap.Instance.NotifyListChanged();
			};
		}
	}

	private void Sleep(Vector3 position)
	{
		Ped[] array = World.GetNearbyPeds(position, _enemyRangeForSleeping).Where(IsEnemy).ToArray();
		if (!array.Any())
		{
			TimeSpan currentDayTime = World.CurrentTimeOfDay + new TimeSpan(0, _sleepHours, 0, 0);
			PlayerPed.IsVisible = false;
			Player.CanControlCharacter = false;
			PlayerPed.IsPositionFrozen = true;
			GTA.UI.Screen.FadeOut(2000);
			Wait(2000);
			World.CurrentTimeOfDay = currentDayTime;
			PlayerPed.IsVisible = true;
			Player.CanControlCharacter = true;
			PlayerPed.IsPositionFrozen = false;
			PlayerPed.ClearBloodDamage();
			Weather[] source = (Weather[])Enum.GetValues(typeof(Weather));
			source = source.Where((Weather w) => w != Weather.Blizzard && w != Weather.Christmas && w != Weather.Snowing && w != Weather.Snowlight && w != Weather.Unknown).ToArray();
			World.Weather = source[Database.Random.Next(source.Length)];
			Wait(2000);
			GTA.UI.Screen.FadeIn(2000);
		}
		else
		{
			Notifier.Show("There are ~r~enemies~s~ nearby.");
			Notifier.Show("Marking them on your map.");
			Array.ForEach(array, AddBlip);
		}
	}

	private static void AddBlip(Ped ped)
	{
		if (!ped.AttachedBlip.Exists())
		{
			Blip blip = ped.AddBlip();
			blip.Name = "Enemy Ped";
			EntityEventWrapper entityEventWrapper = new EntityEventWrapper(ped);
			entityEventWrapper.Died += delegate(EntityEventWrapper sender, Entity entity)
			{
				entity.AttachedBlip?.Delete();
				sender.Dispose();
			};
			entityEventWrapper.Aborted += delegate(EntityEventWrapper sender, Entity entity)
			{
				entity.AttachedBlip?.Delete();
			};
		}
	}

	private static bool IsEnemy(Ped ped)
	{
		// Precedence fix: the alive/human checks now also gate the in-combat case
		// (previously a dead/animal ped flagged in-combat counted as an enemy).
		return ped.IsHuman && !ped.IsDead
			&& (ped.GetRelationshipWithPed(PlayerPed) == Relationship.Hate || ped.IsInCombatAgainst(PlayerPed));
	}
}
