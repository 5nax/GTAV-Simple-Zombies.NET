using System;
using System.Collections.Generic;
using GTA;
using ZombiesMod.DataClasses;
using ZombiesMod.Static;
using ZombiesMod.SurvivorTypes;

namespace ZombiesMod.Controllers;

public class SurvivorController : Script, ISpawner
{
	public delegate void OnCreatedSurvivorsEvent();

	private readonly List<Survivors> _survivors = new List<Survivors>();

	private readonly int _survivorInterval = 30;

	private readonly float _survivorSpawnChance = 0.7f;

	private readonly int _merryweatherTimeout = 120;

	private DateTime _currentDelayTime;

	public bool Spawn { get; set; }

	public static SurvivorController Instance { get; private set; }

	public event OnCreatedSurvivorsEvent CreatedSurvivors;

	public SurvivorController()
	{
		Instance = this;
		_survivorInterval = base.Settings.GetValue("survivors", "survivor_interval", _survivorInterval);
		_survivorSpawnChance = base.Settings.GetValue("survivors", "survivor_spawn_chance", _survivorSpawnChance);
		_merryweatherTimeout = base.Settings.GetValue("survivors", "merryweather_timeout", _merryweatherTimeout);
		base.Settings.SetValue("survivors", "survivor_interval", _survivorInterval);
		base.Settings.SetValue("survivors", "survivor_spawn_chance", _survivorSpawnChance);
		base.Settings.SetValue("survivors", "merryweather_timeout", _merryweatherTimeout);
		base.Settings.Save();
		base.Tick += OnTick;
		base.Aborted += delegate
		{
			_survivors.ForEach(delegate(Survivors s)
			{
				s.Abort();
			});
		};
		CreatedSurvivors += OnCreatedSurvivors;
	}

	private void OnCreatedSurvivors()
	{
		// Honor the configured spawn chance (was computed but discarded, so events
		// fired every interval regardless of survivor_spawn_chance).
		if (Database.Random.NextDouble() > (double)_survivorSpawnChance)
		{
			return;
		}
		EventTypes[] array = (EventTypes[])Enum.GetValues(typeof(EventTypes));
		EventTypes eventTypes = array[Database.Random.Next(array.Length)];
		Survivors survivors = null;
		switch (eventTypes)
		{
		case EventTypes.Friendly:
		{
			FriendlySurvivors survivors4 = new FriendlySurvivors();
			survivors = TryCreateEvent(survivors4);
			break;
		}
		case EventTypes.Hostile:
			if (Database.Random.NextDouble() <= 0.20000000298023224)
			{
				HostileSurvivors survivors3 = new HostileSurvivors();
				survivors = TryCreateEvent(survivors3);
			}
			break;
		case EventTypes.Merryweather:
		{
			MerryweatherSurvivors survivors2 = new MerryweatherSurvivors(_merryweatherTimeout);
			survivors = TryCreateEvent(survivors2);
			break;
		}
		}
		if (survivors != null)
		{
			_survivors.Add(survivors);
			survivors.SpawnEntities();
			survivors.Completed += delegate(Survivors survivors5)
			{
				SetDelayTime();
				survivors5.CleanUp();
				_survivors.Remove(survivors5);
			};
		}
	}

	private Survivors TryCreateEvent(Survivors survivors)
	{
		Survivors result = null;
		if (_survivors.FindIndex((Survivors s) => s.GetType() == survivors.GetType()) <= -1)
		{
			result = survivors;
		}
		return result;
	}

	private void OnTick(object sender, EventArgs eventArgs)
	{
		Create();
		Destroy();
		// Iterate a snapshot: a survivor's Update can complete the event, whose handler
		// removes it from _survivors (mutation-during-enumeration would otherwise throw).
		foreach (Survivors s in new List<Survivors>(_survivors))
		{
			s.Update();
		}
	}

	private void Destroy()
	{
		if (!Spawn)
		{
			// Snapshot + Clear instead of removing inside ForEach (which threw
			// InvalidOperationException and only aborted a subset).
			foreach (Survivors s in new List<Survivors>(_survivors))
			{
				s.Abort();
			}
			_survivors.Clear();
			_currentDelayTime = DateTime.UtcNow;
		}
	}

	private void Create()
	{
		if (Spawn)
		{
			DateTime utcNow = DateTime.UtcNow;
			if (!(utcNow <= _currentDelayTime))
			{
				this.CreatedSurvivors?.Invoke();
				SetDelayTime();
			}
		}
	}

	private void SetDelayTime()
	{
		_currentDelayTime = DateTime.UtcNow + new TimeSpan(0, 0, _survivorInterval);
	}
}
