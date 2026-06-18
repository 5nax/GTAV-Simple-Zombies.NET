using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using ZombiesMod.Extensions;
using ZombiesMod.PlayerManagement;
using ZombiesMod.Static;

namespace ZombiesMod.Scripts;

public class Loot247 : Script, ISpawner
{
	public const float InteractDistance = 1.5f;

	private readonly List<Blip> _blips = new List<Blip>();

	private readonly List<Prop> _lootedShelfes = new List<Prop>();

	private readonly int[] _propHashes;

	public static Loot247 Instance { get; private set; }

	public bool Spawn { get; set; }

	private static Vector3 PlayerPosition => Database.PlayerPosition;

	private static Ped PlayerPed => Database.PlayerPed;

	public Loot247()
	{
		Instance = this;
		_propHashes = new int[5]
		{
			Game.GenerateHash("v_ret_247shelves01"),
			Game.GenerateHash("v_ret_247shelves02"),
			Game.GenerateHash("v_ret_247shelves03"),
			Game.GenerateHash("v_ret_247shelves04"),
			Game.GenerateHash("v_ret_247shelves05")
		};
		base.Tick += OnTick;
		base.Aborted += delegate
		{
			Clear();
		};
	}

	private void OnTick(object sender, EventArgs e)
	{
		SpawnBlips();
		ClearBlips();
		LootShops();
	}

	private void LootShops()
	{
		if (!Spawn || PlayerPed.IsPlayingAnim("oddjobs@shop_robbery@rob_till", "loop"))
		{
			return;
		}
		IEnumerable<Prop> source = World.GetNearbyProps(PlayerPosition, 15f).Where(IsShelf);
		Prop closest = World.GetClosest(PlayerPosition, source.ToArray());
		if (closest == null)
		{
			return;
		}
		float num = closest.Position.VDist(PlayerPosition);
		if (!(num > 1.5f))
		{
			Game.DisableControlThisFrame(Control.Context);
			UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to loot the shelf.");
			if (Ctrl.DisabledJustPressed(Control.Context))
			{
				_lootedShelfes.Add(closest);
				bool flag = Database.Random.NextDouble() > 0.30000001192092896;
				PlayerInventory.Instance.PickupItem(flag ? PlayerInventory.Instance.ItemFromName("Packaged Food") : PlayerInventory.Instance.ItemFromName("Clean Water"), ItemType.Item);
				PlayerPed.Task.PlayAnimation("oddjobs@shop_robbery@rob_till", "loop");
				PlayerPed.Heading = (closest.Position - PlayerPosition).ToHeading();
			}
		}
	}

	private bool IsShelf(Prop arg)
	{
		return _propHashes.Contains(arg.Model.Hash) && !_lootedShelfes.Contains(arg);
	}

	private void ClearBlips()
	{
		if (!Spawn)
		{
			Clear();
		}
	}

	private void SpawnBlips()
	{
		if (Spawn && _blips.Count < Database.Shops247Locations.Length)
		{
			Vector3[] shops247Locations = Database.Shops247Locations;
			foreach (Vector3 position in shops247Locations)
			{
				Blip blip = World.CreateBlip(position);
				blip.Sprite = BlipSprite.Store;
				blip.Name = "Store";
				blip.IsShortRange = true;
				_blips.Add(blip);
			}
		}
	}

	public void Clear()
	{
		while (_blips.Count > 0)
		{
			Blip blip = _blips[0];
			if (blip != null && blip.Exists())
			{
				blip.Delete();
			}
			_blips.RemoveAt(0);
		}
		// Reset looted-shelf tracking so the list can't grow unbounded over a long
		// session (and shelves restock when the system is re-enabled).
		_lootedShelfes.Clear();
	}
}
