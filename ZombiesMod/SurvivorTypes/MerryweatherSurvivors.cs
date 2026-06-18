using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.TimerBars;
using ZombiesMod.DataClasses;
using ZombiesMod.Extensions;
using ZombiesMod.PlayerManagement;
using ZombiesMod.Static;
using ZombiesMod.Wrappers;
using ParticleEffect = ZombiesMod.DataClasses.ParticleEffect;

namespace ZombiesMod.SurvivorTypes;

public class MerryweatherSurvivors : Survivors
{
	private enum DropType
	{
		Weapons,
		Loot
	}

	public const float InteractDistance = 2.3f;

	private const int BlipRadius = 145;

	private readonly int _timeOut;

	private ParticleEffect _particle;

	private readonly PedGroup _pedGroup = new PedGroup();

	private readonly List<Ped> _peds = new List<Ped>();

	private Blip _blip;

	private Prop _prop;

	private DropType _dropType;

	private bool _notify;

	private Vector3 _dropZone;

	private readonly TimerBarProgress _timerBar;

	private float _currentTime;

	private readonly PedHash[] _pedHashes = new PedHash[3]
	{
		PedHash.Blackops01SMY,
		PedHash.Blackops01SMY,
		PedHash.Blackops03SMY
	};

	private readonly WeaponHash[] _weapons = new WeaponHash[5]
	{
		WeaponHash.AdvancedRifle,
		WeaponHash.AssaultRifle,
		WeaponHash.AssaultSMG,
		WeaponHash.AssaultShotgun,
		WeaponHash.BullpupRifle
	};

	public MerryweatherSurvivors(int timeout)
	{
		_timerBar = new TimerBarProgress("TIME LEFT");
		_timeOut = timeout;
		_currentTime = _timeOut;
	}

	public override void Update()
	{
		if (_prop == null)
		{
			return;
		}
		TryInteract(_prop);
		UpdateTimer();
		if (CantSeeCrate())
		{
			return;
		}
		Blip blip = _prop.AddBlip();
		if (!(blip == null))
		{
			blip.Sprite = BlipSprite.CrateDrop;
			blip.Color = BlipColor.Yellow;
			blip.Name = "Crate Drop";
			_blip.Delete();
			_peds.ForEach(delegate(Ped ped)
			{
				Blip blip2 = ped.AddBlip();
				blip2.Color = BlipColor.Yellow;
				blip2.Name = "Merryweather Security";
			});
		}
	}

	private bool CantSeeCrate()
	{
		return !_prop.IsOnScreen || _prop.IsOccluded || _prop.AttachedBlip.Exists() || _prop.Position.VDist(base.PlayerPosition) > 50f;
	}

	private void UpdateTimer()
	{
		if (base.PlayerPosition.VDist(_dropZone) < 145f)
		{
			if (!_notify)
			{
				GTA.UI.Screen.ShowSubtitle("~r~Entering Hostile Zone", 3000);
				_notify = true;
			}
			if (MenuConrtoller.BarPool.Contains(_timerBar))
			{
				MenuConrtoller.BarPool.Remove(_timerBar);
			}
			return;
		}
		if (!MenuConrtoller.BarPool.Contains(_timerBar))
		{
			MenuConrtoller.BarPool.Add(_timerBar);
		}
		_timerBar.Progress = ((_timeOut > 0) ? _currentTime / _timeOut : 0f) * 100f;
		_currentTime -= Game.LastFrameTime;
		if (!(_currentTime > 0f))
		{
			Complete();
			Notifier.Show("~r~Failed~s~ to retrieve crate.");
		}
	}

	public override void SpawnEntities()
	{
		Vector3 spawnPoint = GetSpawnPoint();
		if (!IsValidSpawn(spawnPoint))
		{
			return;
		}
		DropType[] array = (DropType[])Enum.GetValues(typeof(DropType));
		DropType dropType = array[Database.Random.Next(array.Length)];
		_dropType = dropType;
		string text = ((_dropType == DropType.Weapons) ? "prop_mil_crate_01" : "ex_prop_crate_closed_bc");
		_prop = World.CreateProp(text, spawnPoint, Vector3.Zero, dynamic: false, placeOnGround: false);
		if (_prop == null)
		{
			Complete();
			return;
		}
		_blip = World.CreateBlip(_prop.Position.Around(45f), 145f);
		_blip.Color = BlipColor.Yellow;
		_blip.Alpha = 150;
		_dropZone = _blip.Position;
		Vector3 coord = _prop.Position.Around(5f);
		_particle = WorldExtended.CreateParticleEffectAtCoord(coord, "exp_grd_flare");
		_particle.Color = Color.LightGoldenrodYellow;
		int num = Database.Random.Next(3, 6);
		for (int i = 0; i < num; i++)
		{
			Vector3 position = spawnPoint.Around(10f);
			PedHash pedHash = _pedHashes[Database.Random.Next(_pedHashes.Length)];
			Ped ped = World.CreatePed(pedHash, position);
			if (!(ped == null))
			{
				if (i > 0)
				{
					ped.Weapons.Give(_weapons[Database.Random.Next(_weapons.Length)], 45, equipNow: true, isAmmoLoaded: true);
				}
				else
				{
					ped.Weapons.Give(WeaponHash.SniperRifle, 15, equipNow: true, isAmmoLoaded: true);
				}
				ped.Accuracy = 100;
				ped.Task.GuardCurrentPosition();
				ped.RelationshipGroup = Relationships.MilitiaRelationship;
				_pedGroup.Add(ped, i == 0);
				_peds.Add(ped);
				EntityEventWrapper entityEventWrapper = new EntityEventWrapper(ped);
				entityEventWrapper.Died += PedWrapperOnDied;
			}
		}
		// Flavor vehicle near the drop. Mark it non-needed so the engine despawns it
		// naturally instead of leaking a persistent vehicle that nothing tracks.
		World.CreateVehicle("mesa3", World.GetNextPositionOnStreet(_prop.Position.Around(25f)))?.MarkAsNoLongerNeeded();
		Notifier.Show(string.Format("~y~Merryweather~s~ {0} drop nearby.", (_dropType == DropType.Loot) ? "loot" : "weapons"));
	}

	private void PedWrapperOnDied(EntityEventWrapper sender, Entity entity)
	{
		_peds.Remove(entity as Ped);
		entity.MarkAsNoLongerNeeded();
		entity.AttachedBlip?.Delete();
		sender.Dispose();
	}

	private void TryInteract(Entity prop)
	{
		if (prop == null || prop.Position.VDist(base.PlayerPosition) >= 2.3f)
		{
			return;
		}
		UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to loot.");
		Game.DisableControlThisFrame(Control.Context);
		if (!Ctrl.DisabledJustPressed(Control.Context))
		{
			return;
		}
		prop.Delete();
		switch (_dropType)
		{
		case DropType.Loot:
		{
			int num3 = Database.Random.Next(1, 3);
			PlayerInventory.Instance.PickupLoot(null, ItemType.Item, num3, num3, 0.4f);
			break;
		}
		case DropType.Weapons:
		{
			int num = Database.Random.Next(3, 5);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				WeaponHash[] array = ((WeaponHash[])Enum.GetValues(typeof(WeaponHash))).Where(IsGoodHash).ToArray();
				if (array.Length != 0)
				{
					WeaponHash hash = array[Database.Random.Next(array.Length)];
					base.PlayerPed.Weapons.Give(hash, Database.Random.Next(20, 45), equipNow: false, isAmmoLoaded: true);
					num2++;
				}
			}
			Notifier.Show($"Found ~g~{num2}~s~ weapons.");
			break;
		}
		}
		Complete();
	}

	public override void CleanUp()
	{
		_particle?.Delete();
		_peds?.ForEach(delegate(Ped ped)
		{
			ped.AttachedBlip?.Delete();
			ped.AlwaysKeepTask = true;
			ped.IsPersistent = false;
		});
		_blip?.Delete();
		if (MenuConrtoller.BarPool.Contains(_timerBar))
		{
			MenuConrtoller.BarPool.Remove(_timerBar);
		}
	}

	public override void Abort()
	{
		_particle?.Delete();
		_prop?.Delete();
		_peds?.ForEach(delegate(Ped ped)
		{
			ped.AttachedBlip?.Delete();
			ped.Delete();
		});
		_blip?.Delete();
		if (MenuConrtoller.BarPool.Contains(_timerBar))
		{
			MenuConrtoller.BarPool.Remove(_timerBar);
		}
	}

	private bool IsGoodHash(WeaponHash hash)
	{
		return hash != WeaponHash.Unarmed && hash != WeaponHash.BZGas && hash != WeaponHash.Ball && hash != WeaponHash.Snowball && !base.PlayerPed.Weapons.HasWeapon(hash);
	}
}
