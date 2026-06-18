using System;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using ZombiesMod.Extensions;
using ZombiesMod.Scripts;
using ZombiesMod.Static;

namespace ZombiesMod.PlayerManagement;

public class PlayerMap : Script
{
	public delegate void InteractedEvent(MapProp mapProp, InventoryItemBase inventoryItem);

	public const float InteractDistance = 3f;

	private Map _map;

	public static PlayerMap Instance { get; private set; }

	public bool EditMode { get; set; } = true;

	public Vector3 PlayerPosition => Database.PlayerPosition;

	public static event InteractedEvent Interacted;

	public PlayerMap()
	{
		Instance = this;
		base.Tick += OnTick;
		base.Aborted += OnAborted;
		PlayerInventory.BuildableUsed += InventoryOnBuildableUsed;
	}

	public void Deserialize()
	{
		if (_map == null)
		{
			Map map = Serializer.Deserialize<Map>(Config.MapFilePath);
			if (map == null)
			{
				map = new Map();
			}
			_map = map;
			_map.ListChanged += delegate
			{
				Serializer.Serialize(Config.MapFilePath, _map);
			};
			LoadProps();
		}
	}

	private void LoadProps()
	{
		if (_map.Count <= 0)
		{
			return;
		}
		foreach (MapProp item in _map)
		{
			Model model = item.PropName;
			if (!model.Request(1000))
			{
				Notifier.Show($"Tried to request ~y~{item.PropName}~s~ but failed.");
				continue;
			}
			Vector3 position = item.Position;
			Prop prop = (Prop)GTA.Entity.FromHandle(Function.Call<int>((Hash)0x9A294B2138ABB884uL, model.Hash, position.X, position.Y, position.Z, 1, 1, false));
			if (prop == null)
			{
				continue;
			}
			prop.IsPositionFrozen = !item.IsDoor;
			prop.Rotation = item.Rotation;
			item.Handle = prop.Handle;
			if (item.BlipSprite != BlipSprite.Standard)
			{
				Blip blip = prop.AddBlip();
				blip.Sprite = item.BlipSprite;
				blip.Color = item.BlipColor;
				blip.Name = item.Id;
				ZombieVehicleSpawner.Instance.SpawnBlocker.Add(item.Position);
			}
		}
	}

	private void InventoryOnBuildableUsed(BuildableInventoryItem item, Prop newProp)
	{
		if (_map == null)
		{
			Deserialize();
		}
		MapProp mapProp = new MapProp(item.Id, item.PropName, item.BlipSprite, item.BlipColor, item.GroundOffset, item.Interactable, item.IsDoor, item.CanBePickedUp, newProp.Rotation, newProp.Position, newProp.Handle, (item as WeaponStorageInventoryItem)?.WeaponsList);
		_map.Add(mapProp);
		ZombieVehicleSpawner.Instance.SpawnBlocker.Add(mapProp.Position);
	}

	private void OnAborted(object sender, EventArgs eventArgs)
	{
		// Null when the player never pressed Load and never placed a buildable.
		_map?.Clear();
	}

	private void OnTick(object sender, EventArgs eventArgs)
	{
		if (_map == null || !_map.Any() || MenuConrtoller.MenuPool.AreAnyVisible)
		{
			return;
		}
		MapProp closest = World.GetClosest(PlayerPosition, _map.ToArray());
		if (closest != null && closest.CanBePickedUp)
		{
			float num = closest.Position.VDist(PlayerPosition);
			if (!(num > 3f))
			{
				TryUseMapProp(closest);
			}
		}
	}

	private void TryUseMapProp(MapProp mapProp)
	{
		bool flag = mapProp.CanBePickedUp && EditMode;
		if (flag || mapProp.Interactable)
		{
			if (flag)
			{
				Game.DisableControlThisFrame(Control.Context);
			}
			if (mapProp.Interactable)
			{
				DisableAttackActions();
			}
			GameExtended.DisableWeaponWheel();
			UiExtended.DisplayHelpTextThisFrame(string.Format("{0}", flag ? $"Press ~INPUT_CONTEXT~ to pickup the {mapProp.Id}.\n" : ((!EditMode) ? "You're not in edit mode.\n" : "")) + string.Format("{0}", mapProp.Interactable ? string.Format("Press ~INPUT_ATTACK~ to {0}.", mapProp.IsDoor ? "Lock/Unlock" : "interact") : ""));
			if (Ctrl.DisabledJustPressed(Control.Attack) && mapProp.Interactable)
			{
				PlayerMap.Interacted?.Invoke(mapProp, PlayerInventory.Instance.ItemFromName(mapProp.Id));
			}
			if (Ctrl.DisabledJustPressed(Control.Context) && mapProp.CanBePickedUp && PlayerInventory.Instance.PickupItem(PlayerInventory.Instance.ItemFromName(mapProp.Id), ItemType.Item))
			{
				mapProp.Delete();
				_map.Remove(mapProp);
				ZombieVehicleSpawner.Instance.SpawnBlocker.Remove(mapProp.Position);
			}
		}
	}

	public bool Find(Prop prop)
	{
		return _map != null && _map.Contains(prop);
	}

	private static void DisableAttackActions()
	{
		Game.DisableControlThisFrame(Control.Attack2);
		Game.DisableControlThisFrame(Control.Attack);
		Game.DisableControlThisFrame(Control.Aim);
	}

	public void NotifyListChanged()
	{
		_map.NotifyListChanged();
	}
}
