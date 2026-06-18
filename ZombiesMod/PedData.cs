using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using ZombiesMod.PlayerManagement;

namespace ZombiesMod;

[Serializable]
public class PedData : ISpatial, IHandleable, IDeletable
{
	public int Handle { get; set; }

	public int Hash { get; set; }

	public Vector3 Rotation { get; set; }

	public Vector3 Position { get; set; }

	public PedTask Task { get; set; }

	public List<Weapon> Weapons { get; set; }

	public PedData(int handle, int hash, Vector3 rotation, Vector3 position, PedTask task, List<Weapon> weapons)
	{
		Handle = handle;
		Hash = hash;
		Rotation = rotation;
		Position = position;
		Task = task;
		Weapons = weapons;
	}

	public bool Exists()
	{
		return Function.Call<bool>(GTA.Native.Hash._0x7239B21A38F536BA, new InputArgument[1] { Handle });
	}

	public unsafe void Delete()
	{
		int handle = Handle;
		Function.Call(GTA.Native.Hash._0xAE3CBE5BF394C9C9, new InputArgument[1] { &handle });
		Handle = handle;
	}
}
