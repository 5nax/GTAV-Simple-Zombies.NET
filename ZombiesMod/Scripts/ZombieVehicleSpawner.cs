using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using ZombiesMod.DataClasses;
using ZombiesMod.Extensions;
using ZombiesMod.Static;

namespace ZombiesMod.Scripts;

public class ZombieVehicleSpawner : Script, ISpawner
{
	public const int SpawnBlockingDistance = 150;

	private readonly int _maxVehicles = 10;

	private readonly int _maxZombies = 30;

	private readonly int _minVehicles = 1;

	private readonly int _minZombies = 7;

	private readonly int _spawnDistance = 75;

	private readonly int _minSpawnDistance = 50;

	private readonly int _zombieHealth = 750;

	private bool _nightFall;

	private List<Ped> _peds = new List<Ped>();

	private List<Vehicle> _vehicles = new List<Vehicle>();

	private readonly VehicleClass[] _classes = new VehicleClass[3]
	{
		VehicleClass.OffRoad,
		VehicleClass.Muscle,
		VehicleClass.Sedans
	};

	public string[] InvalidZoneNames = new string[8] { "Los Santos International Airport", "Fort Zancudo", "Bolingbroke Penitentiary", "Davis Quartz", "Palmer-Taylor Power Station", "RON Alternates Wind Farm", "Terminal", "Humane Labs and Research" };

	public bool Spawn { get; set; }

	public SpawnBlocker SpawnBlocker { get; } = new SpawnBlocker();

	// Default health applied to spawned zombies, exposed so other systems (hordes,
	// infection spread) create consistent zombies.
	public int DefaultZombieHealth => _zombieHealth;

	// Register an externally-created zombie so it is counted, pruned, and cleared
	// with the rest (used by HordeController and infection-spread).
	public void AddManagedZombie(Ped zombiePed)
	{
		if (zombiePed != null)
		{
			_peds.Add(zombiePed);
		}
	}

	private static Ped PlayerPed => Database.PlayerPed;

	private static Vector3 PlayerPosition => Database.PlayerPosition;

	public static ZombieVehicleSpawner Instance { get; private set; }

	public ZombieVehicleSpawner()
	{
		Instance = this;
		_minZombies = base.Settings.GetValue("spawning", "min_spawned_zombies", _minZombies);
		_maxZombies = base.Settings.GetValue("spawning", "max_spawned_zombies", _maxZombies);
		_minVehicles = base.Settings.GetValue("spawning", "min_spawned_vehicles", _minVehicles);
		_maxVehicles = base.Settings.GetValue("spawning", "max_spawned_vehicles", _maxVehicles);
		_spawnDistance = base.Settings.GetValue("spawning", "spawn_distance", _spawnDistance);
		_minSpawnDistance = base.Settings.GetValue("spawning", "min_spawn_distance", _minSpawnDistance);
		_zombieHealth = base.Settings.GetValue("zombies", "zombie_health", _zombieHealth);
		base.Settings.SetValue("spawning", "min_spawned_zombies", _minZombies);
		base.Settings.SetValue("spawning", "max_spawned_zombies", _maxZombies);
		base.Settings.SetValue("spawning", "min_spawned_vehicles", _minVehicles);
		base.Settings.SetValue("spawning", "max_spawned_vehicles", _maxVehicles);
		base.Settings.SetValue("spawning", "spawn_distance", _spawnDistance);
		base.Settings.SetValue("spawning", "min_spawn_distance", _minSpawnDistance);
		base.Settings.SetValue("zombies", "zombie_health", _zombieHealth);
		base.Settings.Save();
		base.Tick += OnTick;
		base.Aborted += delegate
		{
			ClearAll();
		};
		base.Interval = 100;
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (Spawn)
		{
			if (!MenuConrtoller.MenuPool.AreAnyVisible)
			{
				if (ZombieCreator.IsNightFall() && !_nightFall)
				{
					UiExtended.DisplayHelpTextThisFrame("Nightfall approaches. Zombies are far more ~r~aggressive~s~ at night.");
					_nightFall = true;
				}
				else if (!ZombieCreator.IsNightFall())
				{
					_nightFall = false;
				}
			}
			SpawnVehicles();
			SpawnPeds();
		}
		else
		{
			ClearAll();
		}
	}

	private void SpawnPeds()
	{
		_peds = _peds.Where(Exists).ToList();
		if (_peds.Count >= _maxZombies)
		{
			return;
		}
		for (int i = 0; i < _maxZombies - _peds.Count; i++)
		{
			Vector3 position = PlayerPed.Position.Around(_spawnDistance);
			position = World.GetNextPositionOnStreet(position);
			if (!IsValidSpawn(position))
			{
				break;
			}
			Vector3 vector = position.Around(5f);
			if (vector.IsOnScreen() || vector.VDist(PlayerPosition) < (float)_minSpawnDistance)
			{
				break;
			}
			Ped ped = World.CreateRandomPed(vector);
			if (!(ped == null))
			{
				_peds.Add(ZombieCreator.InfectPed(ped, _zombieHealth));
			}
		}
	}

	private void SpawnVehicles()
	{
		_vehicles = _vehicles.Where(Exists).ToList();
		if (_vehicles.Count >= _maxVehicles)
		{
			return;
		}
		for (int i = 0; i < _maxVehicles - _vehicles.Count; i++)
		{
			Vector3 position = PlayerPed.Position.Around(_spawnDistance);
			position = World.GetNextPositionOnStreet(position);
			if (IsInvalidZone(position) || !IsValidSpawn(position))
			{
				break;
			}
			Vector3 vector = position.Around(2.5f);
			if (vector.IsOnScreen() || vector.VDist(PlayerPosition) < (float)_minSpawnDistance)
			{
				break;
			}
			Model randomVehicleModel = Database.GetRandomVehicleModel();
			Vehicle vehicle = World.CreateVehicle(randomVehicleModel, vector);
			if (!(vehicle == null))
			{
				vehicle.EngineHealth = 0f;
				vehicle.MarkAsNoLongerNeeded();
				vehicle.DirtLevel = 14f;
				SmashRandomWindow(vehicle);
				if (Database.Random.NextDouble() < 0.5)
				{
					SmashRandomWindow(vehicle);
				}
				if (Database.Random.NextDouble() < 0.20000000298023224)
				{
					OpenRandomDoor(vehicle);
				}
				vehicle.Heading = Database.Random.Next(1, 360);
				_vehicles.Add(vehicle);
			}
		}
	}

	private static void OpenRandomDoor(Vehicle veh)
	{
		VehicleDoorIndex[] present = ((VehicleDoorIndex[])Enum.GetValues(typeof(VehicleDoorIndex)))
			.Where((VehicleDoorIndex d) => veh.Doors.Contains(d)).ToArray();
		if (present.Length == 0)
		{
			return;
		}
		VehicleDoorIndex door = present[Database.Random.Next(present.Length)];
		veh.Doors[door].Open(loose: false, instantly: true);
	}

	private static void SmashRandomWindow(Vehicle veh)
	{
		VehicleWindowIndex[] intact = ((VehicleWindowIndex[])Enum.GetValues(typeof(VehicleWindowIndex)))
			.Where((VehicleWindowIndex w) => Function.Call<bool>((Hash)0x46E571A0E20D01F1uL, veh.Handle, (int)w)).ToArray();
		if (intact.Length == 0)
		{
			return;
		}
		VehicleWindowIndex window = intact[Database.Random.Next(intact.Length)];
		veh.Windows[window].Smash();
	}

	public bool IsInvalidZone(Vector3 spawn)
	{
		string zone = Function.Call<string>((Hash)0x7EE64D51E8498728uL, spawn.X, spawn.Y, spawn.Z);
		return Array.Find(InvalidZoneNames, (string z) => z == zone) != null;
	}

	private static bool Exists(Entity arg)
	{
		return arg != null && arg.Exists();
	}

	private void ClearAll()
	{
		while (_peds.Count > 0)
		{
			Ped ped = _peds[0];
			ped.Delete();
			_peds.RemoveAt(0);
		}
		while (_vehicles.Count > 0)
		{
			Vehicle vehicle = _vehicles[0];
			vehicle.Delete();
			_vehicles.RemoveAt(0);
		}
	}

	public bool IsValidSpawn(Vector3 spawnPoint)
	{
		int num = SpawnBlocker.FindIndex((Vector3 spawn) => spawn.VDist(spawnPoint) < 150f);
		return num <= -1;
	}
}
