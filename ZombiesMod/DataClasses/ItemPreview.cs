using System;
using GTA;
using GTA.Math;
using GTA.Native;
using ZombiesMod.Extensions;
using ZombiesMod.Scripts;
using ZombiesMod.Static;

namespace ZombiesMod.DataClasses;

public class ItemPreview
{
	private Vector3 _currentOffset;

	private Prop _currentPreview;

	private Prop _resultProp;

	private bool _preview;

	private bool _isDoor;

	private string _currnetPropHash;

	public bool PreviewComplete { get; private set; }

	public ItemPreview()
	{
		ScriptEventHandler.Instance.RegisterScript(OnTick);
		ScriptEventHandler.Instance.Aborted += delegate
		{
			Abort();
		};
	}

	public void OnTick(object sender, EventArgs eventArgs)
	{
		if (_preview)
		{
			CreateItemPreview();
		}
	}

	public Prop GetResult()
	{
		return _resultProp;
	}

	public void StartPreview(string propHash, Vector3 offset, bool isDoor)
	{
		if (!_preview)
		{
			_preview = true;
			_currnetPropHash = propHash;
			_isDoor = isDoor;
		}
	}

	private void CreateItemPreview()
	{
		if (_currentPreview == null)
		{
			PreviewComplete = false;
			_currentOffset = Vector3.Zero;
			Prop prop = World.CreateProp(_currnetPropHash, default(Vector3), default(Vector3), dynamic: false, placeOnGround: false);
			if (prop == null)
			{
				Notifier.Show($"Failed to load prop, even after request.\nProp Name: {_currnetPropHash}");
				_resultProp = null;
				_preview = false;
				PreviewComplete = true;
			}
			else
			{
				prop.IsCollisionEnabled = false;
				_currentPreview = prop;
				_currentPreview.Opacity = 150;
				Database.PlayerPed.Weapons.Select(WeaponHash.Unarmed, equipNow: true);
				_resultProp = null;
			}
			return;
		}
		UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_AIM~ to cancel.\nPress ~INPUT_ATTACK~ to place the item.", ignoreMenus: true);
		Game.DisableControlThisFrame(Control.Aim);
		Game.DisableControlThisFrame(Control.Attack);
		Game.DisableControlThisFrame(Control.Attack2);
		Game.DisableControlThisFrame(Control.ParachuteBrakeLeft);
		Game.DisableControlThisFrame(Control.ParachuteBrakeRight);
		Game.DisableControlThisFrame(Control.Cover);
		Game.DisableControlThisFrame(Control.Phone);
		Game.DisableControlThisFrame(Control.PhoneUp);
		Game.DisableControlThisFrame(Control.PhoneDown);
		Game.DisableControlThisFrame(Control.Sprint);
		GameExtended.DisableWeaponWheel();
		if (Ctrl.DisabledPressed(Control.Aim))
		{
			_currentPreview.Delete();
			_currentPreview = (_resultProp = null);
			_preview = false;
			PreviewComplete = true;
			ScriptEventHandler.Instance.UnregisterScript(OnTick);
			return;
		}
		Vector3 position = GameplayCamera.Position;
		Vector3 direction = GameplayCamera.Direction;
		Vector3 hitCoords = World.Raycast(position, position + direction * 15f, IntersectFlags.Everything, Database.PlayerPed).HitPosition;
		if (hitCoords != Vector3.Zero && hitCoords.DistanceTo(Database.PlayerPosition) > 1.5f)
		{
			DrawScaleForms();
			float num = (Game.IsControlPressed(Control.Sprint) ? 1.5f : 1f);
			if (Game.IsControlPressed(Control.ParachuteBrakeLeft))
			{
				Vector3 rotation = _currentPreview.Rotation;
				rotation.Z += Game.LastFrameTime * 50f * num;
				_currentPreview.Rotation = rotation;
			}
			else if (Game.IsControlPressed(Control.ParachuteBrakeRight))
			{
				Vector3 rotation2 = _currentPreview.Rotation;
				rotation2.Z -= Game.LastFrameTime * 50f * num;
				_currentPreview.Rotation = rotation2;
			}
			if (Game.IsControlPressed(Control.PhoneUp))
			{
				_currentOffset.Z += Game.LastFrameTime * num;
			}
			else if (Game.IsControlPressed(Control.PhoneDown))
			{
				_currentOffset.Z -= Game.LastFrameTime * num;
			}
			_currentPreview.Position = hitCoords + _currentOffset;
			_currentPreview.IsVisible = true;
			if (Ctrl.DisabledJustPressed(Control.Attack))
			{
				_currentPreview.ResetOpacity();
				_resultProp = _currentPreview;
				_resultProp.IsCollisionEnabled = true;
				_resultProp.IsPositionFrozen = !_isDoor;
				_preview = false;
				_currentPreview = null;
				_currnetPropHash = string.Empty;
				PreviewComplete = true;
				ScriptEventHandler.Instance.UnregisterScript(OnTick);
			}
		}
		else
		{
			_currentPreview.IsVisible = false;
		}
	}

	private static void DrawScaleForms()
	{
		Scaleform scaleform = new Scaleform("instructional_buttons");
		scaleform.CallFunction("CLEAR_ALL");
		scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
		scaleform.CallFunction("CREATE_CONTAINER");
		scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>((Hash)0x0499D7B09FC9B407uL, new InputArgument[3] { 2, 152, 0 }), string.Empty);
		scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>((Hash)0x0499D7B09FC9B407uL, new InputArgument[3] { 2, 153, 0 }), "Rotate");
		scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>((Hash)0x0499D7B09FC9B407uL, new InputArgument[3] { 2, 172, 0 }), string.Empty);
		scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>((Hash)0x0499D7B09FC9B407uL, new InputArgument[3] { 2, 173, 0 }), "Lift/Lower");
		scaleform.CallFunction("SET_DATA_SLOT", 4, Function.Call<string>((Hash)0x0499D7B09FC9B407uL, new InputArgument[3] { 2, 21, 0 }), "Accelerate");
		scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
		scaleform.Render2D();
	}

	public void Abort()
	{
		_currentPreview?.Delete();
	}
}
