using System;
using GTA;
using GTA.Native;
using ZombiesMod.Extensions;
using ZombiesMod.Static;
using ZombiesMod.Zombies;
using ZombiesMod.Zombies.ZombieTypes;

namespace ZombiesMod.Scripts;

public static class ZombieCreator
{
	public static bool Runners { get; set; }

	public static ZombiePed InfectPed(Ped ped, int health, bool overrideAsFastZombie = false)
	{
		ped.CanPlayGestures = false;
		ped.SetCanPlayAmbientAnims(toggle: false);
		ped.SetCanEvasiveDive(toggle: false);
		ped.SetPathCanUseLadders(toggle: false);
		ped.SetPathCanClimb(toggle: false);
		ped.DisablePainAudio(toggle: true);
		ped.ApplyDamagePack(0f, 1f, DamagePack.BigHitByVehicle);
		ped.ApplyDamagePack(0f, 1f, DamagePack.ExplosionMed);
		ped.AlwaysDiesOnLowHealth = false;
		ped.SetAlertness(Alertness.Nuetral);
		ped.SetCombatAttributes(CombatAttributes.AlwaysFight, enabled: true);
		Function.Call(Hash._0x70A2D1137C8ED7C9, new InputArgument[3] { ped.Handle, 0, 0 });
		ped.SetConfigFlag(281, value: true);
		ped.Task.WanderAround(ped.Position, ZombiePed.WanderRadius);
		ped.AlwaysKeepTask = true;
		ped.BlockPermanentEvents = true;
		ped.IsPersistent = false;
		ped.CurrentBlip?.Remove();
		ped.IsPersistent = true;
		ped.RelationshipGroup = Relationships.InfectedRelationship;
		float num = 0.055f;
		if (IsNightFall())
		{
			num = 0.5f;
		}
		TimeSpan currentDayTime = World.CurrentDayTime;
		if (currentDayTime.Hours >= 20 || currentDayTime.Hours <= 3)
		{
			num = 0.4f;
		}
		if ((Database.Random.NextDouble() < (double)num || overrideAsFastZombie) && Runners)
		{
			return new Runner(ped.Handle);
		}
		int health2 = (ped.MaxHealth = health);
		ped.Health = health2;
		return new Walker(ped.Handle);
	}

	public static bool IsNightFall()
	{
		if (!Runners)
		{
			return false;
		}
		TimeSpan currentDayTime = World.CurrentDayTime;
		return currentDayTime.Hours >= 20 || currentDayTime.Hours <= 3;
	}
}
