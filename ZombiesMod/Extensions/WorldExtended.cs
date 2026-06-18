using GTA.Math;
using GTA.Native;
using ZombiesMod.DataClasses;
using ZombiesMod.Static;

namespace ZombiesMod.Extensions;

public static class WorldExtended
{
	public static void SetParkedVehicleDensityMultiplierThisFrame(float multiplier)
	{
		Function.Call(Hash._0xEAE6DCC7EEE3DB1D, new InputArgument[1] { multiplier });
	}

	public static void SetVehicleDensityMultiplierThisFrame(float multiplier)
	{
		Function.Call(Hash._0x245A6883D966D537, new InputArgument[1] { multiplier });
	}

	public static void SetRandomVehicleDensityMultiplierThisFrame(float multiplier)
	{
		Function.Call(Hash._0xB3B3359379FE77D3, new InputArgument[1] { multiplier });
	}

	public static void SetPedDensityThisMultiplierFrame(float multiplier)
	{
		Function.Call(Hash._0x95E3D6257B166CF2, new InputArgument[1] { multiplier });
	}

	public static void SetScenarioPedDensityThisMultiplierFrame(float multiplier)
	{
		Function.Call(Hash._0x7A556143A1C03898, new InputArgument[1] { multiplier });
	}

	public static void RemoveAllShockingEvents(bool toggle)
	{
		Function.Call(Hash._0xEAABE8FDFA21274C, new InputArgument[1] { toggle ? 1 : 0 });
	}

	public static void SetFrontendRadioActive(bool active)
	{
		Function.Call(Hash._0xF7F26C6E9CC9EBB8, new InputArgument[1] { active ? 1 : 0 });
	}

	public static void ClearCops(float radius = 9000f)
	{
		Vector3 playerPosition = Database.PlayerPosition;
		Function.Call(Hash._0x04F8FC8FCF58F88D, new InputArgument[5] { playerPosition.X, playerPosition.Y, playerPosition.Z, radius, 0 });
	}

	public static void ClearAreaOfEverything(Vector3 position, float radius)
	{
		Function.Call(Hash._0x957838AAF91BD12D, new InputArgument[8] { position.X, position.Y, position.Z, radius, false, false, false, false });
	}

	public static ParticleEffect CreateParticleEffectAtCoord(Vector3 coord, string name)
	{
		Function.Call(Hash._0x6C38AF3693A69A91, new InputArgument[1] { "core" });
		int handle = Function.Call<int>(Hash._0xE184F4F0DC5910E7, new InputArgument[12]
		{
			name, coord.X, coord.Y, coord.Z, 0.0, 0.0, 0.0, 1f, 0, 0,
			0, 0
		});
		return new ParticleEffect(handle);
	}
}
