using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Menus;
using ZombiesMod.Extensions;
using ZombiesMod.Static;
using ZombiesMod.Wrappers;

namespace ZombiesMod.PlayerManagement;

public class PlayerGroupManager : Script
{
	private readonly NativeMenu _pedMenu;

	private Ped _selectedPed;

	private PedCollection _peds;

	private readonly Dictionary<Ped, PedTask> _pedTasks = new Dictionary<Ped, PedTask>();

	public Ped PlayerPed => Database.PlayerPed;

	public Vector3 PlayerPosition => Database.PlayerPosition;

	public static PlayerGroupManager Instance { get; private set; }

	public PlayerGroupManager()
	{
		Instance = this;
		_pedMenu = new NativeMenu("Guard", "SELECT AN OPTION");
		MenuConrtoller.MenuPool.Add(_pedMenu);
		_pedMenu.Closed += delegate
		{
			_selectedPed = null;
		};
		NativeListItem<string> tasksItem = new NativeListItem<string>("Tasks", "Give peds a specific task to perform.", Enum.GetNames(typeof(PedTask)));
		tasksItem.Activated += delegate
		{
			if (!(_selectedPed == null))
			{
				PedTask index = (PedTask)tasksItem.SelectedIndex;
				SetTask(_selectedPed, index);
			}
		};
		NativeItem uIMenuItem = new NativeItem("Apply To Nearby", "Apply the selected task to nearby peds within 50 meters.");
		uIMenuItem.Activated += delegate
		{
			PedTask task = (PedTask)tasksItem.SelectedIndex;
			List<Ped> list = PlayerPed.PedGroup.Where((Ped ped) => ped.Position.VDist(PlayerPosition) < 50f).ToList();
			list.ForEach(delegate(Ped ped)
			{
				SetTask(ped, task);
			});
		};
		NativeItem uIMenuItem2 = new NativeItem("Give Weapon", "Give this ped your current weapon.");
		uIMenuItem2.Activated += delegate
		{
			if (!(_selectedPed == null))
			{
				TradeWeapons(PlayerPed, _selectedPed);
			}
		};
		NativeItem uIMenuItem3 = new NativeItem("Take Weapon", "Take the ped's current weapon.");
		uIMenuItem3.Activated += delegate
		{
			if (!(_selectedPed == null))
			{
				TradeWeapons(_selectedPed, PlayerPed);
			}
		};
		NativeListItem<string> globalTasks = new NativeListItem<string>("Guard Tasks", "Give all guards a specific task to perform.", Enum.GetNames(typeof(PedTask)));
		globalTasks.Activated += delegate
		{
			PedTask task = (PedTask)globalTasks.SelectedIndex;
			List<Ped> list = PlayerPed.PedGroup.ToList();
			list.ForEach(delegate(Ped ped)
			{
				SetTask(ped, task);
			});
		};
		_pedMenu.Add(tasksItem);
		_pedMenu.Add(uIMenuItem);
		_pedMenu.Add(uIMenuItem2);
		_pedMenu.Add(uIMenuItem3);
		ModController.Instance.MainMenu.Add(globalTasks);
		Tick += OnTick;
		Aborted += OnAborted;
	}

	private void SetTask(Ped ped, PedTask task)
	{
		if (task == (PedTask)(-1) || ped.IsPlayer)
		{
			return;
		}
		if (!_pedTasks.ContainsKey(ped))
		{
			_pedTasks.Add(ped, task);
		}
		else
		{
			_pedTasks[ped] = task;
		}
		ped.Task.ClearAll();
		switch (task)
		{
		case PedTask.Chill:
			Function.Call((Hash)0x277F471BA9DB000BuL, new InputArgument[6]
			{
				ped.Handle,
				ped.Position.X,
				ped.Position.Y,
				ped.Position.Z,
				100f,
				-1
			});
			break;
		case PedTask.Combat:
			ped.Task.FightAgainstHatedTargets(100f);
			break;
		case PedTask.Guard:
			ped.Task.GuardCurrentPosition();
			break;
		case PedTask.StandStill:
			ped.Task.StandStill(-1);
			break;
		case PedTask.Leave:
			ped.LeaveGroup();
			ped.AttachedBlip?.Delete();
			ped.MarkAsNoLongerNeeded();
			EntityEventWrapper.Dispose(ped);
			break;
		case PedTask.VehicleFollow:
		{
			Vehicle closestVehicle = World.GetClosestVehicle(ped.Position, 100f);
			if (closestVehicle == null)
			{
				Notifier.Show("There's no vehicle near this ped.", blinking: true);
				return;
			}
			Function.Call((Hash)0xFC545A9F0626E3B6uL, new InputArgument[6] { ped.Handle, closestVehicle.Handle, PlayerPed.Handle, 1074528293, 262144, 15 });
			break;
		}
		}
		ped.BlockPermanentEvents = task == PedTask.Follow;
	}

	private void OnAborted(object sender, EventArgs eventArgs)
	{
		PedGroup currentPedGroup = PlayerPed.PedGroup;
		List<Ped> list = currentPedGroup.Where((Ped ped2) => !ped2.IsPlayer).ToList();
		currentPedGroup.Dispose();
		while (list.Count > 0)
		{
			Ped ped = list[0];
			ped.Delete();
			list.RemoveAt(0);
		}
	}

	private void OnTick(object sender, EventArgs eventArgs)
	{
		if (PlayerPed.IsInVehicle() || MenuConrtoller.MenuPool.AreAnyVisible || PlayerPed.PedGroup.MemberCount <= 0)
		{
			return;
		}
		Ped[] nearbyPeds = World.GetNearbyPeds(PlayerPed, 1.5f);
		Ped closest = World.GetClosest(PlayerPosition, nearbyPeds);
		if (!(closest == null) && !closest.IsInVehicle() && !(closest.PedGroup != PlayerPed.PedGroup))
		{
			Game.DisableControlThisFrame(Control.Context);
			UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to configure this ped.");
			if (Ctrl.DisabledJustPressed(Control.Context))
			{
				_selectedPed = closest;
				_pedMenu.Visible = !_pedMenu.Visible;
			}
		}
	}

	public void Deserialize()
	{
		if (_peds != null)
		{
			return;
		}
		PedCollection pedCollection = Serializer.Deserialize<PedCollection>("./scripts/Guards.dat");
		if (pedCollection == null)
		{
			pedCollection = new PedCollection();
		}
		_peds = pedCollection;
		_peds.ListChanged += delegate
		{
			Serializer.Serialize("./scripts/Guards.dat", _peds);
		};
		_peds.ToList().ForEach(delegate(PedData data)
		{
			Ped ped = World.CreatePed(data.Hash, data.Position);
			if (!(ped == null))
			{
				ped.Rotation = data.Rotation;
				ped.Recruit(PlayerPed);
				data.Weapons.ForEach(delegate(Weapon w)
				{
					ped.Weapons.Give(w.Hash, w.Ammo, equipNow: true, isAmmoLoaded: true);
				});
				data.Handle = ped.Handle;
				SetTask(ped, data.Task);
			}
		});
	}

	public void SavePeds()
	{
		if (_peds == null)
		{
			Deserialize();
		}
		List<Ped> list = PlayerPed.PedGroup.ToList(includingLeader: false);
		if (list.Count <= 0)
		{
			Notifier.Show("You have no bodyguards.");
			return;
		}
		List<PedData> pedDatas = _peds.ToList();
		List<PedData> list2 = list.ConvertAll(delegate(Ped ped)
		{
			PedData data = pedDatas.Find((PedData pedData) => pedData.Handle == ped.Handle);
			return UpdatePedData(ped, data);
		}).ToList();
		list2.ForEach(delegate(PedData data)
		{
			if (!_peds.Contains(data))
			{
				_peds.Add(data);
			}
		});
		Serializer.Serialize("./scripts/Guards.dat", _peds);
		Notifier.Show("~b~Guards~s~ saved!");
	}

	private PedData UpdatePedData(Ped ped, PedData data)
	{
		PedTask task = (_pedTasks.ContainsKey(ped) ? _pedTasks[ped] : ((PedTask)(-1)));
		IEnumerable<WeaponHash> source = ((WeaponHash[])Enum.GetValues(typeof(WeaponHash))).Where((WeaponHash hash) => ped.Weapons.HasWeapon(hash));
		WeaponComponentHash[] componentHashes = (WeaponComponentHash[])Enum.GetValues(typeof(WeaponComponentHash));
		List<Weapon> weapons = source.ToList().ConvertAll(delegate(WeaponHash hash)
		{
			GTA.Weapon weapon = ped.Weapons[hash];
			WeaponComponentHash[] components = componentHashes.Where((WeaponComponentHash h) => weapon.Components[h].Active).ToArray();
			return new Weapon(weapon.Ammo, weapon.Hash, components);
		}).ToList();
		switch (data == null)
		{
		case true:
			data = new PedData(ped.Handle, ped.Model.Hash, ped.Rotation, ped.Position, task, weapons);
			break;
		case false:
			data.Position = ped.Position;
			data.Rotation = ped.Rotation;
			data.Task = task;
			data.Weapons = weapons;
			break;
		}
		return data;
	}

	private static void TradeWeapons(Ped trader, Ped reviever)
	{
		if (trader.Weapons.Current == trader.Weapons[WeaponHash.Unarmed])
		{
			return;
		}
		GTA.Weapon current = trader.Weapons.Current;
		if (!reviever.Weapons.HasWeapon(current.Hash))
		{
			if (!reviever.IsPlayer)
			{
				reviever.Weapons.Drop();
			}
			GTA.Weapon weapon = reviever.Weapons.Give(current.Hash, 0, equipNow: true, isAmmoLoaded: true);
			weapon.Ammo = current.Ammo;
			weapon.InfiniteAmmo = false;
			trader.Weapons.Remove(current);
		}
	}
}
