using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using LemonUI.TimerBars;
using ZombiesMod.Extensions;
using ZombiesMod.Static;

namespace ZombiesMod.PlayerManagement;

// Per-vehicle fuel: drains while driving, shown on a HUD bar, mirrored to the in-game
// fuel gauge, and the engine cuts out when empty. Refuel by standing the vehicle next
// to a gas pump or by using a crafted Fuel Can (ItemEvent.Refuel).
public class VehicleFuel : Script
{
	private static readonly Model[] GasPumps = new Model[]
	{
		"prop_gas_pump_1a", "prop_gas_pump_1b", "prop_gas_pump_1c",
		"prop_gas_pump_1d", "prop_gas_pump_old2", "prop_gas_pump_old3", "prop_vintage_pump"
	};

	public static VehicleFuel Instance { get; private set; }

	private readonly Dictionary<int, float> _fuel = new Dictionary<int, float>();

	private readonly TimerBarProgress _bar;

	private bool _barShown;

	public VehicleFuel()
	{
		Instance = this;
		_bar = new TimerBarProgress("FUEL")
		{
			ForegroundColor = Color.FromArgb(230, 170, 40),
			BackgroundColor = Color.FromArgb(60, 50, 30)
		};
		Tick += OnTick;
	}

	public void Refuel(float amount)
	{
		Vehicle veh = Database.PlayerCurrentVehicle;
		if (veh == null || !veh.Exists())
		{
			Notifier.Show("You're not in a vehicle to refuel.");
			return;
		}
		_fuel[veh.Handle] = Math.Min(GameConfig.FuelCapacity, GetFuel(veh) + amount);
		Notifier.Show($"~g~Refueled~s~ (+{(int)amount}).");
	}

	private float GetFuel(Vehicle veh)
	{
		if (!_fuel.TryGetValue(veh.Handle, out var f))
		{
			f = GameConfig.FuelCapacity; // assume a found vehicle starts full
			_fuel[veh.Handle] = f;
		}
		return f;
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!GameConfig.FuelEnabled || !Database.PlayerInVehicle)
		{
			HideBar();
			return;
		}
		Vehicle veh = Database.PlayerCurrentVehicle;
		if (veh == null || !veh.Exists() || Database.PlayerPed != veh.Driver)
		{
			HideBar();
			return;
		}

		float fuel = GetFuel(veh);

		// Drain while the engine runs, faster at speed.
		if (veh.IsEngineRunning && fuel > 0f)
		{
			fuel -= GameConfig.FuelDrainPerSecond * (1f + veh.Speed * 0.05f) * Game.LastFrameTime;
		}

		// Auto-refuel when stationary beside a gas pump.
		if (veh.Speed < 0.5f && World.GetNearbyProps(veh.Position, 6f, GasPumps).Length > 0)
		{
			UiExtended.DisplayHelpTextThisFrame("Refueling at the pump...");
			fuel += 12f * Game.LastFrameTime;
		}

		fuel = Math.Max(0f, Math.Min(GameConfig.FuelCapacity, fuel));
		_fuel[veh.Handle] = fuel;
		veh.FuelLevel = fuel / GameConfig.FuelCapacity * 65f; // mirror to the dashboard gauge

		if (fuel <= 0f)
		{
			veh.IsEngineRunning = false;
			UiExtended.DisplayHelpTextThisFrame("~r~Out of fuel.~s~ Find a gas pump or a fuel can.");
		}

		ShowBar();
		_bar.Progress = fuel / GameConfig.FuelCapacity * 100f;
	}

	private void ShowBar()
	{
		if (!_barShown)
		{
			MenuConrtoller.BarPool.Add(_bar);
			_barShown = true;
		}
	}

	private void HideBar()
	{
		if (_barShown)
		{
			MenuConrtoller.BarPool.Remove(_bar);
			_barShown = false;
		}
	}
}
