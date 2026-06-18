using System.Collections.Generic;
using GTA;
using GTA.Math;
using ZombiesMod.DataClasses;
using ZombiesMod.Extensions;
using ZombiesMod.Static;
using ZombiesMod.Wrappers;

namespace ZombiesMod.SurvivorTypes;

public class HostileSurvivors : Survivors
{
	private readonly PedGroup _group = new PedGroup();

	private readonly List<Ped> _peds = new List<Ped>();

	private Vehicle _vehicle;

	public override void Update()
	{
		if (_peds.Count <= 0)
		{
			Complete();
		}
	}

	public override void SpawnEntities()
	{
		Vector3 spawnPoint = GetSpawnPoint();
		if (!IsValidSpawn(spawnPoint))
		{
			return;
		}
		Vehicle vehicle = World.CreateVehicle(Database.GetRandomVehicleModel(), spawnPoint, Database.Random.Next(1, 360));
		if (vehicle == null)
		{
			Complete();
			return;
		}
		_vehicle = vehicle;
		Blip blip = _vehicle.AddBlip();
		blip.Name = "Enemy Vehicle";
		blip.Sprite = BlipSprite.PersonalVehicleCar;
		blip.Color = BlipColor.Red;
		EntityEventWrapper entityEventWrapper = new EntityEventWrapper(_vehicle);
		entityEventWrapper.Died += VehicleWrapperOnDied;
		entityEventWrapper.Updated += VehicleWrapperOnUpdated;
		for (int i = 0; i < vehicle.PassengerSeats + 1; i++)
		{
			if (_group.MemberCount < 6 && vehicle.IsSeatFree(VehicleSeat.Any))
			{
				Ped ped = vehicle.CreateRandomPedOnSeat((i == 0) ? VehicleSeat.Driver : VehicleSeat.Any);
				if (!(ped == null))
				{
					ped.Weapons.Give(Database.WeaponHashes[Database.Random.Next(Database.WeaponHashes.Length)], 25, equipNow: true, isAmmoLoaded: true);
					ped.SetCombatAttributes(CombatAttributes.AlwaysFight, enabled: true);
					ped.SetAlertness(Alertness.FullyAlert);
					ped.RelationshipGroup = Relationships.HostileRelationship;
					_group.Add(ped, i == 0);
					Blip blip2 = ped.AddBlip();
					blip2.Name = "Enemy";
					_peds.Add(ped);
					EntityEventWrapper entityEventWrapper2 = new EntityEventWrapper(ped);
					entityEventWrapper2.Died += PedWrapperOnDied;
					entityEventWrapper2.Updated += PedWrapperOnUpdated;
					entityEventWrapper2.Disposed += PedWrapperOnDisposed;
				}
			}
		}
		UI.Notify("~r~Hostiles~s~ nearby!");
	}

	private void PedWrapperOnDisposed(EntityEventWrapper sender, Entity entity)
	{
		if (_peds.Contains(entity as Ped))
		{
			_peds.Remove(entity as Ped);
		}
	}

	private void VehicleWrapperOnUpdated(EntityEventWrapper sender, Entity entity)
	{
		if (!(entity == null))
		{
			entity.CurrentBlip.Alpha = (_vehicle.Driver.Exists() ? 255 : 0);
		}
	}

	private void VehicleWrapperOnDied(EntityEventWrapper sender, Entity entity)
	{
		entity.CurrentBlip?.Remove();
		sender.Dispose();
		_vehicle.MarkAsNoLongerNeeded();
		_vehicle = null;
	}

	private void PedWrapperOnUpdated(EntityEventWrapper sender, Entity entity)
	{
		Ped ped = entity as Ped;
		if (!(ped == null))
		{
			if (ped.CurrentVehicle?.Driver == ped && !ped.IsInCombat)
			{
				ped.Task.DriveTo(ped.CurrentVehicle, base.PlayerPosition, 25f, 75f);
			}
			if (ped.Position.VDist(base.PlayerPosition) > Survivors.DeleteRange)
			{
				ped.Delete();
			}
			if (ped.CurrentBlip.Exists())
			{
				ped.CurrentBlip.Alpha = ((!ped.IsInVehicle()) ? 255 : 0);
			}
		}
	}

	private void PedWrapperOnDied(EntityEventWrapper sender, Entity entity)
	{
		entity.CurrentBlip?.Remove();
		_peds.Remove(entity as Ped);
	}

	public override void CleanUp()
	{
		_vehicle?.CurrentBlip?.Remove();
		EntityEventWrapper.Dispose(_vehicle);
	}

	public override void Abort()
	{
		_vehicle?.Delete();
		while (_peds.Count > 0)
		{
			_peds[0]?.Delete();
			_peds.RemoveAt(0);
		}
	}
}
