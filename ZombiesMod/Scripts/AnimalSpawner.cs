using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using ZombiesMod.Extensions;
using ZombiesMod.Static;

namespace ZombiesMod.Scripts;

public class AnimalSpawner : Script, ISpawner
{
	public const int MinAnimalsPerSpawn = 3;

	public const int MaxAnimalsPerSpawn = 10;

	public const int RespawnDistance = 200;

	private readonly PedHash[] _possibleAnimals = new PedHash[5]
	{
		PedHash.Deer,
		PedHash.Boar,
		PedHash.Coyote,
		PedHash.Cow,
		PedHash.Pig
	};

	private readonly List<Blip> _spawnBlips = new List<Blip>();

	private Dictionary<Blip, List<Ped>> _spawnMap = new Dictionary<Blip, List<Ped>>();

	private int _nextFleeTime;

	public static AnimalSpawner Instance { get; private set; }

	public bool Spawn { get; set; }

	public AnimalSpawner()
	{
		Instance = this;
		base.Tick += OnTick;
		base.Aborted += OnAborted;
	}

	private void OnAborted(object sender, EventArgs e)
	{
		Clear();
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (Spawn)
		{
			CreateBlips();
			int i = 0;
			for (int count = _spawnBlips.Count; i < count; i++)
			{
				Blip blip = _spawnBlips[i];
				if (!_spawnMap.ContainsKey(blip))
				{
					List<Ped> value = CreateAnimals(blip);
					_spawnMap.Add(blip, value);
					continue;
				}
				List<Ped> list = _spawnMap[blip];
				for (int num = list.Count - 1; num >= 0; num--)
				{
					Ped ped = list[num];
					if (ped == null)
					{
						list.Remove(null);
					}
					else if (ped.IsDead || !ped.Exists())
					{
						ped.MarkAsNoLongerNeeded();
						list.Remove(ped);
					}
				}
			}
			TryFleeFromPlayer();
			_spawnMap = _spawnMap.Where(delegate(KeyValuePair<Blip, List<Ped>> keyValuePair)
			{
				if (keyValuePair.Value.Count != 0)
				{
					return true;
				}
				float num2 = keyValuePair.Key.Position.VDist(Database.PlayerPosition);
				return !(num2 > 200f);
			}).ToDictionary((KeyValuePair<Blip, List<Ped>> keyValuePair) => keyValuePair.Key, (KeyValuePair<Blip, List<Ped>> keyValuePair) => keyValuePair.Value);
		}
		else
		{
			Clear();
		}
	}

	// Skittish prey: animals bolt when the player gets close, so you must stalk and
	// shoot (which then makes noise that draws the dead — risk/reward hunting).
	private void TryFleeFromPlayer()
	{
		if (!GameConfig.HuntingEnabled || Game.GameTime < _nextFleeTime)
		{
			return;
		}
		_nextFleeTime = Game.GameTime + 2000;
		Vector3 playerPos = Database.PlayerPosition;
		foreach (KeyValuePair<Blip, List<Ped>> kv in _spawnMap)
		{
			foreach (Ped animal in kv.Value)
			{
				if (animal != null && animal.Exists() && !animal.IsDead
					&& animal.Position.VDist(playerPos) < 30f)
				{
					animal.Task.FleeFrom(Database.PlayerPed);
				}
			}
		}
	}

	private List<Ped> CreateAnimals(Blip blip)
	{
		List<Ped> list = new List<Ped>();
		int num = Database.Random.Next(3, MaxAnimalsPerSpawn + 1);
		PedHash pedHash = _possibleAnimals[Database.Random.Next(_possibleAnimals.Length)];
		// Request the model up-front; without this CreatePed returns null until the
		// model streams in, so herds were often empty/undersized.
		Model model = new Model(pedHash);
		model.Request(1000);
		if (!model.IsLoaded)
		{
			return list;
		}
		for (int i = 0; i < num; i++)
		{
			Ped ped = World.CreatePed(model, blip.Position.Around(5f));
			if (!(ped == null))
			{
				list.Add(ped);
				ped.Task.WanderAround();
				ped.IsPersistent = true;
				// Prey, not enemies: skittish wildlife that flees and dies to a clean shot.
				ped.CanSufferCriticalHits = true;
			}
		}
		return list;
	}

	private void CreateBlips()
	{
		if (_spawnBlips.Count < Database.AnimalSpawns.Length)
		{
			int i = 0;
			for (int num = Database.AnimalSpawns.Length; i < num; i++)
			{
				Vector3 position = Database.AnimalSpawns[i];
				Blip blip = World.CreateBlip(position);
				blip.Sprite = BlipSprite.Hunting;
				blip.Name = "Animals";
				_spawnBlips.Add(blip);
			}
		}
	}

	private void Clear()
	{
		if (_spawnBlips.Count > 0)
		{
			foreach (Blip spawnBlip in _spawnBlips)
			{
				if (_spawnMap.ContainsKey(spawnBlip))
				{
					List<Ped> list = _spawnMap[spawnBlip];
					foreach (Ped item in list)
					{
						item.Delete();
					}
				}
				spawnBlip.Delete();
			}
		}
		_spawnMap.Clear();
	}
}
