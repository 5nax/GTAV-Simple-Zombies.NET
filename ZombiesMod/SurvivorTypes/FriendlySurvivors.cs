using System.Collections.Generic;
using GTA;
using GTA.Math;
using ZombiesMod.DataClasses;
using ZombiesMod.Extensions;
using ZombiesMod.Static;
using ZombiesMod.Wrappers;

namespace ZombiesMod.SurvivorTypes;

public class FriendlySurvivors : Survivors
{
	private readonly List<Ped> _peds = new List<Ped>();

	private readonly PedGroup _pedGroup = new PedGroup();

	public static FriendlySurvivors Instance { get; private set; }

	public FriendlySurvivors()
	{
		Instance = this;
	}

	public void RemovePed(Ped item)
	{
		if (_peds.Contains(item))
		{
			_peds.Remove(item);
			item.LeaveGroup();
			item.CurrentBlip?.Remove();
			EntityEventWrapper.Dispose(item);
		}
	}

	public override void Update()
	{
		if (_peds.Count <= 0)
		{
			Complete();
		}
	}

	public override void SpawnEntities()
	{
		int num = Database.Random.Next(3, 6);
		Vector3 spawnPoint = GetSpawnPoint();
		if (!IsValidSpawn(spawnPoint))
		{
			return;
		}
		for (int i = 0; i <= num; i++)
		{
			Ped ped = World.CreateRandomPed(spawnPoint.Around(5f));
			if (!(ped == null))
			{
				Blip blip = ped.AddBlip();
				blip.Color = BlipColor.Blue;
				blip.Name = "Friendly";
				ped.RelationshipGroup = Relationships.FriendlyRelationship;
				ped.Task.FightAgainstHatedTargets(9000f);
				ped.SetAlertness(Alertness.FullyAlert);
				ped.SetCombatAttributes(CombatAttributes.AlwaysFight, enabled: true);
				ped.Weapons.Give(Database.WeaponHashes[Database.Random.Next(Database.WeaponHashes.Length)], 25, equipNow: true, isAmmoLoaded: true);
				_pedGroup.Add(ped, i == 0);
				_pedGroup.FormationType = FormationType.Default;
				_peds.Add(ped);
				EntityEventWrapper entityEventWrapper = new EntityEventWrapper(ped);
				entityEventWrapper.Died += EventWrapperOnDied;
				entityEventWrapper.Disposed += EventWrapperOnDisposed;
			}
		}
		UI.Notify("~b~Friendly~s~ survivors nearby.", blinking: true);
	}

	private void EventWrapperOnDisposed(EntityEventWrapper sender, Entity entity)
	{
		if (_peds.Contains(entity as Ped))
		{
			_peds.Remove(entity as Ped);
		}
	}

	private void EventWrapperOnDied(EntityEventWrapper sender, Entity entity)
	{
		_peds.Remove(entity as Ped);
		entity.CurrentBlip?.Remove();
		entity.MarkAsNoLongerNeeded();
		sender.Dispose();
	}

	public override void CleanUp()
	{
		_peds.ForEach(delegate(Ped ped)
		{
			ped.CurrentBlip?.Remove();
			ped.MarkAsNoLongerNeeded();
			EntityEventWrapper.Dispose(ped);
		});
	}

	public override void Abort()
	{
		while (_peds.Count > 0)
		{
			Ped ped = _peds[0];
			ped.Delete();
			_peds.RemoveAt(0);
		}
	}
}
