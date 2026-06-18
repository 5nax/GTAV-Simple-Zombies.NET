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
		base.Aborted += OnAborted;
	}

	private void OnAborted(object sender, EventArgs eventArgs)
	{
		_vehicleCollection.ToList().ForEach(delegate(VehicleData vehicle)
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
		VehicleCollection vehicleCollection = Serializer.Deserialize<VehicleCollection>("./scripts/Vehicles.dat");
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
			vehicle.PrimaryColor = item.PrimaryColor;
			vehicle.SecondaryColor = item.SecondaryColor;
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
		if (data != null && !(vehicle == null))
		{
			vehicle.InstallModKit();
			data.NeonLights?.ToList().ForEach(delegate(VehicleNeonLight h)
			{
				vehicle.SetNeonLightsOn(h, on: true);
			});
			data.Mods?.ForEach(delegate(Tuple<VehicleMod, int> m)
			{
				vehicle.SetMod(m.Item1, m.Item2, variations: true);
			});
			data.ToggleMods?.ToList().ForEach(delegate(VehicleToggleMod h)
			{
				vehicle.ToggleMod(h, toggle: true);
			});
			vehicle.WindowTint = data.WindowTint;
			vehicle.WheelType = data.WheelType;
			vehicle.NeonLightsColor = data.NeonColor;
			Function.Call((Hash)0x60BF608F1B8CD1B6uL, new InputArgument[2] { vehicle.Handle, data.Livery });
		}
	}

	public void Serialize(bool notify = false)
	{
		if (_vehicleCollection != null)
		{
			UpdateVehicleData();
			Serializer.Serialize("./scripts/Vehicles.dat", _vehicleCollection);
			if (notify)
			{
				Notifier.Show((_vehicleCollection.Count <= 0) ? "No vehicles." : "~p~Vehicles~s~ saved!");
			}
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
			Vehicle vehicle = _vehicles.Find((Vehicle i) => i.Handle == v.Handle);
			if (!(vehicle == null))
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
		vehicleData.PrimaryColor = vehicle.PrimaryColor;
		vehicleData.SecondaryColor = vehicle.SecondaryColor;
	}

	public void SaveVehicle(Vehicle vehicle)
	{
		if (_vehicleCollection == null)
		{
			Deserialize();
		}
		VehicleData vehicleData = _vehicleCollection.ToList().Find((VehicleData v) => v.Handle == vehicle.Handle);
		if (vehicleData != null)
		{
			UpdateDataSpecific(vehicleData, vehicle);
			Serialize(notify: true);
			return;
		}
		VehicleNeonLight[] source = (VehicleNeonLight[])Enum.GetValues(typeof(VehicleNeonLight));
		source = source.Where(vehicle.IsNeonLightsOn).ToArray();
		VehicleMod[] source2 = (VehicleMod[])Enum.GetValues(typeof(VehicleMod));
		List<Tuple<VehicleMod, int>> mods = new List<Tuple<VehicleMod, int>>();
		source2.ToList().ForEach(delegate(VehicleMod h)
		{
			int mod = vehicle.GetMod(h);
			if (mod != -1)
			{
				mods.Add(new Tuple<VehicleMod, int>(h, mod));
			}
		});
		VehicleToggleMod[] source3 = (VehicleToggleMod[])Enum.GetValues(typeof(VehicleToggleMod));
		source3 = source3.Where(vehicle.IsToggleModOn).ToArray();
		VehicleData item = new VehicleData(vehicle.Handle, vehicle.Model.Hash, vehicle.Rotation, vehicle.Position, vehicle.PrimaryColor, vehicle.SecondaryColor, vehicle.Health, vehicle.EngineHealth, vehicle.Heading, source, mods, source3, vehicle.WindowTint, vehicle.WheelType, vehicle.NeonLightsColor, Function.Call<int>((Hash)0x2BB9230590DA5E8AuL, new InputArgument[1] { vehicle.Handle }), Function.Call<bool>((Hash)0xB3924ECD70E095DCuL, new InputArgument[2] { vehicle.Handle, 23 }), Function.Call<bool>((Hash)0xB3924ECD70E095DCuL, new InputArgument[2] { vehicle.Handle, 24 }));
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
		blip.Name = vehicle.FriendlyName;
		blip.Scale = 0.85f;
	}

	private static BlipSprite GetSprite(Vehicle vehicle)
	{
		return (vehicle.ClassType == VehicleClass.Motorcycles) ? BlipSprite.PersonalVehicleBike : ((vehicle.ClassType == VehicleClass.Boats) ? BlipSprite.Boat : ((vehicle.ClassType == VehicleClass.Helicopters) ? BlipSprite.Helicopter : ((vehicle.ClassType == VehicleClass.Planes) ? BlipSprite.Plane : BlipSprite.PersonalVehicleCar)));
	}

	private void WrapperOnDied(EntityEventWrapper sender, Entity entity)
	{
		Notifier.Show("Your vehicle was ~r~destroyed~s~!");
		_vehicleCollection.Remove(_vehicleCollection.ToList().Find((VehicleData v) => v.Handle == entity.Handle));
		entity.AttachedBlip?.Delete();
		sender.Dispose();
	}
}
