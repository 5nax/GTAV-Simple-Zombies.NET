using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;
using ZombiesMod.Static;
using ZombiesMod.Wrappers;

namespace ZombiesMod.PlayerManagement;

public class PlayerVehicles : Script
{
	private VehicleCollection _vehicleCollection;

	private readonly List<Vehicle> _vehicles = new List<Vehicle>();

	public static PlayerVehicles Instance { get; private set; }

	public PlayerVehicles()
	{
		Instance = this;
		Aborted += OnAborted;
	}

	private void OnAborted(object sender, EventArgs eventArgs)
	{
		_vehicleCollection?.ToList().ForEach(delegate(VehicleData vehicle)
		{
			vehicle.Delete();
		});
	}

	public void Deserialize()
	{
		if (_vehicleCollection != null)
		{
			return;
		}
		VehicleCollection vehicleCollection = Serializer.Deserialize<VehicleCollection>(Config.VehicleFilePath);
		if (vehicleCollection == null)
		{
			vehicleCollection = new VehicleCollection();
		}
		_vehicleCollection = vehicleCollection;
		_vehicleCollection.ListChanged += delegate
		{
			Serialize();
		};
		foreach (VehicleData item in _vehicleCollection)
		{
			Vehicle vehicle = World.CreateVehicle(item.Hash, item.Position);
			if (vehicle == null)
			{
				Notifier.Show("Failed to load vehicle.");
				break;
			}
			vehicle.Mods.PrimaryColor = item.PrimaryColor;
			vehicle.Mods.SecondaryColor = item.SecondaryColor;
			vehicle.Health = item.Health;
			vehicle.EngineHealth = item.EngineHealth;
			vehicle.Rotation = item.Rotation;
			item.Handle = vehicle.Handle;
			AddKit(vehicle, item);
			AddBlipToVehicle(vehicle);
			_vehicles.Add(vehicle);
			vehicle.IsPersistent = true;
			EntityEventWrapper entityEventWrapper = new EntityEventWrapper(vehicle);
			entityEventWrapper.Died += WrapperOnDied;
		}
	}

	private static void AddKit(Vehicle vehicle, VehicleData data)
	{
		if (data == null || vehicle == null)
		{
			return;
		}
		vehicle.Mods.InstallModKit();
		data.NeonLights?.ToList().ForEach(delegate(VehicleNeonLight h)
		{
			vehicle.Mods.SetNeonLightsOn(h, on: true);
		});
		data.Mods?.ForEach(delegate(Tuple<VehicleModType, int> m)
		{
			vehicle.Mods[m.Item1].Index = m.Item2;
		});
		data.ToggleMods?.ToList().ForEach(delegate(VehicleToggleModType h)
		{
			vehicle.Mods[h].IsInstalled = true;
		});
		vehicle.Mods.WindowTint = data.WindowTint;
		vehicle.Mods.WheelType = data.WheelType;
		vehicle.Mods.NeonLightsColor = data.NeonColor;
		// Restore data the original captured but never re-applied (audit fix):
		vehicle.Heading = data.Heading;
		Function.Call(Hash.TOGGLE_VEHICLE_MOD, vehicle.Handle, 23, data.Wheels1);
		Function.Call(Hash.TOGGLE_VEHICLE_MOD, vehicle.Handle, 24, data.Wheels2);
		Function.Call(Hash.SET_VEHICLE_LIVERY, vehicle.Handle, data.Livery);
	}

	public void Serialize(bool notify = false)
	{
		if (_vehicleCollection == null)
		{
			return;
		}
		UpdateVehicleData();
		Serializer.Serialize(Config.VehicleFilePath, _vehicleCollection);
		if (notify)
		{
			Notifier.Show((_vehicleCollection.Count <= 0) ? "No vehicles." : "~p~Vehicles~s~ saved!");
		}
	}

	private void UpdateVehicleData()
	{
		if (_vehicleCollection.Count <= 0)
		{
			return;
		}
		_vehicleCollection.ToList().ForEach(delegate(VehicleData v)
		{
			Vehicle vehicle = _vehicles.Find((Vehicle i) => i != null && i.Exists() && i.Handle == v.Handle);
			if (vehicle != null)
			{
				UpdateDataSpecific(v, vehicle);
			}
		});
	}

	private static void UpdateDataSpecific(VehicleData vehicleData, Vehicle vehicle)
	{
		vehicleData.Position = vehicle.Position;
		vehicleData.Rotation = vehicle.Rotation;
		vehicleData.Health = vehicle.Health;
		vehicleData.EngineHealth = vehicle.EngineHealth;
		vehicleData.PrimaryColor = vehicle.Mods.PrimaryColor;
		vehicleData.SecondaryColor = vehicle.Mods.SecondaryColor;
	}

	public void SaveVehicle(Vehicle vehicle)
	{
		if (_vehicleCollection == null)
		{
			Deserialize();
		}
		VehicleData existing = _vehicleCollection.ToList().Find((VehicleData v) => v.Handle == vehicle.Handle);
		if (existing != null)
		{
			UpdateDataSpecific(existing, vehicle);
			Serialize(notify: true);
			return;
		}
		VehicleNeonLight[] neonLights = ((VehicleNeonLight[])Enum.GetValues(typeof(VehicleNeonLight)))
			.Where(vehicle.Mods.IsNeonLightsOn).ToArray();
		List<Tuple<VehicleModType, int>> mods = new List<Tuple<VehicleModType, int>>();
		foreach (VehicleModType modType in (VehicleModType[])Enum.GetValues(typeof(VehicleModType)))
		{
			int index = vehicle.Mods[modType].Index;
			if (index != -1)
			{
				mods.Add(new Tuple<VehicleModType, int>(modType, index));
			}
		}
		VehicleToggleModType[] toggleMods = ((VehicleToggleModType[])Enum.GetValues(typeof(VehicleToggleModType)))
			.Where((VehicleToggleModType t) => vehicle.Mods[t].IsInstalled).ToArray();
		VehicleData item = new VehicleData(vehicle.Handle, vehicle.Model.Hash, vehicle.Rotation, vehicle.Position,
			vehicle.Mods.PrimaryColor, vehicle.Mods.SecondaryColor, vehicle.Health, vehicle.EngineHealth, vehicle.Heading,
			neonLights, mods, toggleMods, vehicle.Mods.WindowTint, vehicle.Mods.WheelType, vehicle.Mods.NeonLightsColor,
			Function.Call<int>(Hash.GET_VEHICLE_LIVERY, vehicle.Handle),
			Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, vehicle.Handle, 23),
			Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, vehicle.Handle, 24));
		_vehicleCollection.Add(item);
		_vehicles.Add(vehicle);
		vehicle.IsPersistent = true;
		EntityEventWrapper entityEventWrapper = new EntityEventWrapper(vehicle);
		entityEventWrapper.Died += WrapperOnDied;
		AddBlipToVehicle(vehicle);
	}

	private static void AddBlipToVehicle(Vehicle vehicle)
	{
		Blip blip = vehicle.AddBlip();
		blip.Sprite = GetSprite(vehicle);
		blip.Color = BlipColor.PurpleDark;
		blip.Name = vehicle.LocalizedName;
		blip.Scale = 0.85f;
	}

	private static BlipSprite GetSprite(Vehicle vehicle)
	{
		switch (vehicle.ClassType)
		{
		case VehicleClass.Motorcycles:
			return BlipSprite.PersonalVehicleBike;
		case VehicleClass.Boats:
			return BlipSprite.Boat;
		case VehicleClass.Helicopters:
			return BlipSprite.Helicopter;
		case VehicleClass.Planes:
			return BlipSprite.Plane;
		default:
			return BlipSprite.PersonalVehicleCar;
		}
	}

	private void WrapperOnDied(EntityEventWrapper sender, Entity entity)
	{
		Notifier.Show("Your vehicle was ~r~destroyed~s~!");
		_vehicleCollection.Remove(_vehicleCollection.ToList().Find((VehicleData v) => v.Handle == entity.Handle));
		_vehicles.RemoveAll((Vehicle v) => v == null || v.Handle == entity.Handle);
		entity.AttachedBlip?.Delete();
		sender.Dispose();
	}
}
