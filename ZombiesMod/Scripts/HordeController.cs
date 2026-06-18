using System;
using GTA;
using GTA.Math;
using ZombiesMod.Extensions;
using ZombiesMod.Static;
using ZombiesMod.Zombies;
using ZombiesMod.Zombies.ZombieTypes;

namespace ZombiesMod.Scripts;

// Dynamic, escalating threats: periodic ambient hordes that converge on the player,
// "blood moon" nights that crank up aggression, and reinforcement waves summoned by
// Screamers. All gated behind GameConfig + the main Infection-Mode toggle (Spawn).
public class HordeController : Script
{
	public static HordeController Instance { get; private set; }

	private int _nextHordeTime;

	private bool _bloodMoonActive;

	private bool _rolledThisNight;

	private bool _runnersBeforeBloodMoon;

	private static Ped PlayerPed => Database.PlayerPed;

	private static bool Active => ZombieVehicleSpawner.Instance != null && ZombieVehicleSpawner.Instance.Spawn;

	public HordeController()
	{
		Instance = this;
		_nextHordeTime = Game.GameTime + GameConfig.HordeIntervalSeconds * 1000;
		Screamer.Screamed += OnScreamed;
		Tick += OnTick;
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!Active || Database.PlayerIsDead)
		{
			return;
		}
		HandleBloodMoon();
		HandleAmbientHorde();
	}

	private void HandleAmbientHorde()
	{
		if (!GameConfig.HordesEnabled || Game.GameTime < _nextHordeTime)
		{
			return;
		}
		int size = GameConfig.HordeSize * (_bloodMoonActive ? 2 : 1);
		SpawnHorde(PlayerPed.Position, size);
		int interval = GameConfig.HordeIntervalSeconds * (_bloodMoonActive ? 1 : 2) / (_bloodMoonActive ? 2 : 1);
		_nextHordeTime = Game.GameTime + Math.Max(60, interval) * 1000;
		if (!_bloodMoonActive)
		{
			Notifier.Show("~o~You hear a horde approaching...~s~");
		}
	}

	private void HandleBloodMoon()
	{
		if (!GameConfig.BloodMoonEnabled)
		{
			return;
		}
		int hour = World.CurrentTimeOfDay.Hours;
		bool isNight = hour >= 20 || hour <= 4;
		if (isNight && !_rolledThisNight)
		{
			_rolledThisNight = true;
			if (Database.Random.Next(100) < GameConfig.BloodMoonChancePercent)
			{
				StartBloodMoon();
			}
		}
		else if (!isNight)
		{
			_rolledThisNight = false;
			if (_bloodMoonActive)
			{
				EndBloodMoon();
			}
		}
	}

	private void StartBloodMoon()
	{
		_bloodMoonActive = true;
		_runnersBeforeBloodMoon = ZombieCreator.Runners;
		ZombieCreator.Runners = true; // everything sprints during a blood moon
		World.Weather = Weather.Foggy;
		Notifier.Show("~r~BLOOD MOON~s~ — the dead are restless tonight. Run.", blinking: true);
		GTA.UI.Screen.ShowSubtitle("~r~A blood moon rises...", 5000);
	}

	private void EndBloodMoon()
	{
		_bloodMoonActive = false;
		ZombieCreator.Runners = _runnersBeforeBloodMoon;
		Notifier.Show("~g~The blood moon fades.~s~ The horde thins with the dawn.");
	}

	// Called by Screamers to rush reinforcements toward the scream.
	private void OnScreamed(Vector3 position)
	{
		if (Active)
		{
			SpawnHorde(position, 4);
		}
	}

	private void SpawnHorde(Vector3 origin, int count)
	{
		for (int i = 0; i < count; i++)
		{
			SpawnNear(origin, 30f);
		}
	}

	// Create + infect + register one zombie near a point, off-screen where possible.
	public ZombiePed SpawnNear(Vector3 origin, float radius)
	{
		Vector3 spot = World.GetNextPositionOnStreet(origin.Around(radius));
		if (!ZombieVehicleSpawner.Instance.IsValidSpawn(spot) || spot.IsOnScreen())
		{
			return null;
		}
		Ped ped = World.CreateRandomPed(spot);
		if (ped == null)
		{
			return null;
		}
		ZombiePed zombie = ZombieCreator.InfectPed(ped, ZombieVehicleSpawner.Instance.DefaultZombieHealth);
		ZombieVehicleSpawner.Instance.AddManagedZombie(zombie);
		return zombie;
	}
}
