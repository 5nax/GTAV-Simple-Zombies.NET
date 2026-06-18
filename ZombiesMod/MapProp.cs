using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace ZombiesMod;

[Serializable]
public class MapProp : IMapObject, IIdentifier, IProp, ISpatial, IHandleable, IDeletable
{
	public string Id { get; set; }

	public string PropName { get; set; }

	public BlipSprite BlipSprite { get; set; }

	public BlipColor BlipColor { get; set; }

	public Vector3 GroundOffset { get; set; }

	public bool Interactable { get; set; }

	public bool IsDoor { get; set; }

	public bool CanBePickedUp { get; set; }

	public Vector3 Rotation { get; set; }

	public Vector3 Position { get; set; }

	public int Handle { get; set; }

	public List<Weapon> Weapons { get; set; }

	public MapProp(string id, string propName, BlipSprite blipSprite, BlipColor blipColor, Vector3 groundOffset, bool interactable, bool isDoor, bool canBePickedUp, Vector3 rotation, Vector3 position, int handle, List<Weapon> weapons)
	{
		Id = id;
		PropName = propName;
		BlipSprite = blipSprite;
		BlipColor = blipColor;
		GroundOffset = groundOffset;
		Interactable = interactable;
		IsDoor = isDoor;
		CanBePickedUp = canBePickedUp;
		Rotation = rotation;
		Position = position;
		Handle = handle;
		Weapons = weapons;
	}

	public bool Exists()
	{
		return Function.Call<bool>((Hash)0x7239B21A38F536BAuL, new InputArgument[1] { Handle });
	}

	public unsafe void Delete()
	{
		int handle = Handle;
		Function.Call((Hash)0xAE3CBE5BF394C9C9uL, new InputArgument[1] { &handle });
		Handle = handle;
	}
}
