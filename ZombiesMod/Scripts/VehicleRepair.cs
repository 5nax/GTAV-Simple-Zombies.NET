using System;
using GTA;
using GTA.Math;
using GTA.Native;
using ZombiesMod.Extensions;
using ZombiesMod.PlayerManagement;
using ZombiesMod.Static;

namespace ZombiesMod.Scripts;

public class VehicleRepair : Script
{
	private Vehicle _selectedVehicle;

	private InventoryItemBase _item;

	private readonly int _repairTimeMs = 7500;

	private static Ped PlayerPed => Database.PlayerPed;

	public VehicleRepair()
	{
		_repairTimeMs = base.Settings.GetValue("interaction", "vehicle_repair_time_ms", _repairTimeMs);
		base.Settings.SetValue("interaction", "vehicle_repair_time_ms", _repairTimeMs);
		base.Settings.Save();
		base.Tick += OnTick;
		base.Aborted += OnAborted;
	}

	private static void OnAborted(object sender, EventArgs e)
	{
		PlayerPed.Task.ClearAll();
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (Database.PlayerInVehicle)
		{
			return;
		}
		Vehicle closestVehicle = World.GetClosestVehicle(Database.PlayerPosition, 20f);
		if (_item == null)
		{
			_item = PlayerInventory.Instance.ItemFromName("Vehicle Repair Kit");
		}
		if (_selectedVehicle != null)
		{
			Game.DisableControlThisFrame(2, Control.Attack);
			UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_ATTACK~ to cancel.");
			if (Game.IsDisabledControlJustPressed(2, Control.Attack))
			{
				PlayerPed.Task.ClearAllImmediately();
				_selectedVehicle.CloseDoor(VehicleDoor.Hood, instantly: false);
				_selectedVehicle = null;
			}
			else if (PlayerPed.TaskSequenceProgress == -1)
			{
				_selectedVehicle.EngineHealth = 1000f;
				_selectedVehicle.CloseDoor(VehicleDoor.Hood, instantly: false);
				_selectedVehicle = null;
				PlayerInventory.Instance.AddItem(_item, -1, ItemType.Item);
				UI.Notify("Items: -~r~1");
			}
		}
		else
		{
			if (!(closestVehicle != null) || !closestVehicle.Model.IsCar || !(closestVehicle.EngineHealth < 1000f) || MenuConrtoller.MenuPool.IsAnyMenuOpen() || closestVehicle.IsUpsideDown || !closestVehicle.HasBone("engine"))
			{
				return;
			}
			Vector3 boneCoord = closestVehicle.GetBoneCoord(closestVehicle.GetBoneIndex("engine"));
			if (boneCoord == Vector3.Zero || !PlayerPed.IsInRangeOf(boneCoord, 1.5f))
			{
				return;
			}
			if (!PlayerInventory.Instance.HasItem(_item, ItemType.Item))
			{
				UiExtended.DisplayHelpTextThisFrame("You need a vehicle repair kit to fix this engine.");
				return;
			}
			Game.DisableControlThisFrame(2, Control.Context);
			UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to repair engine.");
			if (Game.IsDisabledControlJustPressed(2, Control.Context))
			{
				closestVehicle.OpenDoor(VehicleDoor.Hood, loose: false, instantly: false);
				PlayerPed.Weapons.Select(WeaponHash.Unarmed, equipNow: true);
				Vector3 position = boneCoord + closestVehicle.ForwardVector;
				float heading = (closestVehicle.Position - Database.PlayerPosition).ToHeading();
				TaskSequence taskSequence = new TaskSequence();
				taskSequence.AddTask.ClearAllImmediately();
				taskSequence.AddTask.GoTo(position, ignorePaths: false, 1500);
				taskSequence.AddTask.AchieveHeading(heading, 2000);
				taskSequence.AddTask.PlayAnimation("mp_intro_seq@", "mp_mech_fix", 8f, -8f, _repairTimeMs, AnimationFlags.Loop, 1f);
				taskSequence.AddTask.ClearAll();
				taskSequence.Close();
				PlayerPed.Task.PerformSequence(taskSequence);
				taskSequence.Dispose();
				_selectedVehicle = closestVehicle;
			}
		}
	}
}
