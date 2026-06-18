using System;
using GTA;
using GTA.Native;
using ZombiesMod.Extensions;
using CombatAttributes = ZombiesMod.Extensions.CombatAttributes;
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
		ped.DiesOnLowHealth = false;
		ped.SetAlertness(Alertness.Nuetral);
		ped.SetCombatAttributes(CombatAttributes.AlwaysFight, enabled: true);
		Function.Call((Hash)0x70A2D1137C8ED7C9uL, new InputArgument[3] { ped.Handle, 0, 0 });
		ped.SetConfigFlag(281, value: true);
		ped.Task.WanderAround(ped.Position, ZombiePed.WanderRadius);
		ped.AlwaysKeepTask = true;
		ped.BlockPermanentEvents = true;
		ped.IsPersistent = false;
		ped.AttachedBlip?.Delete();
		ped.IsPersistent = true;
		ped.RelationshipGroup = Relationships.InfectedRelationship;

		// --- Special variants add variety; rolled before the runner/walker split. ---
		double special = Database.Random.NextDouble();
		if (special < GameConfig.BruteChance)
		{
			int big = health * 3;
			ped.MaxHealth = big;
			ped.Health = big;
			return new Brute(ped.Handle);
		}
		if (special < GameConfig.BruteChance + GameConfig.CrawlerChance)
		{
			int low = Math.Max(50, health / 2);
			ped.MaxHealth = low;
			ped.Health = low;
			return new Crawler(ped.Handle);
		}
		if (special < GameConfig.BruteChance + GameConfig.CrawlerChance + GameConfig.BloaterChance)
		{
			ped.MaxHealth = health;
			ped.Health = health;
			return new Bloater(ped.Handle);
		}
		if (special < GameConfig.BruteChance + GameConfig.CrawlerChance + GameConfig.BloaterChance + GameConfig.SpitterChance)
		{
			ped.MaxHealth = health;
			ped.Health = health;
			return new Spitter(ped.Handle);
		}
		if (special < GameConfig.BruteChance + GameConfig.CrawlerChance + GameConfig.BloaterChance + GameConfig.SpitterChance + GameConfig.ScreamerChance)
		{
			ped.MaxHealth = health;
			ped.Health = health;
			return new Screamer(ped.Handle);
		}

		float num = 0.055f;
		if (IsNightFall())
		{
			num = 0.5f;
		}
		TimeSpan currentDayTime = World.CurrentTimeOfDay;
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
		TimeSpan currentDayTime = World.CurrentTimeOfDay;
		return currentDayTime.Hours >= 20 || currentDayTime.Hours <= 3;
	}
}
