using GTA.Math;
using GTA.Native;
using ZombiesMod.DataClasses;
using ZombiesMod.Static;

namespace ZombiesMod.Extensions;

public static class WorldExtended
{
	public static void SetParkedVehicleDensityMultiplierThisFrame(float multiplier)
	{
		Function.Call((Hash)0xEAE6DCC7EEE3DB1DuL, new InputArgument[1] { multiplier });
	}

	public static void SetVehicleDensityMultiplierThisFrame(float multiplier)
	{
		Function.Call((Hash)0x245A6883D966D537uL, new InputArgument[1] { multiplier });
	}

	public static void SetRandomVehicleDensityMultiplierThisFrame(float multiplier)
	{
		Function.Call((Hash)0xB3B3359379FE77D3uL, new InputArgument[1] { multiplier });
	}

	public static void SetPedDensityThisMultiplierFrame(float multiplier)
	{
		Function.Call((Hash)0x95E3D6257B166CF2uL, new InputArgument[1] { multiplier });
	}

	public static void SetScenarioPedDensityThisMultiplierFrame(float multiplier)
	{
		Function.Call((Hash)0x7A556143A1C03898uL, new InputArgument[1] { multiplier });
	}

	public static void RemoveAllShockingEvents(bool toggle)
	{
		Function.Call((Hash)0xEAABE8FDFA21274CuL, new InputArgument[1] { toggle ? 1 : 0 });
	}

	public static void SetFrontendRadioActive(bool active)
	{
		Function.Call((Hash)0xF7F26C6E9CC9EBB8uL, new InputArgument[1] { active ? 1 : 0 });
	}

	public static void ClearCops(float radius = 9000f)
	{
		Vector3 playerPosition = Database.PlayerPosition;
		Function.Call((Hash)0x04F8FC8FCF58F88DuL, new InputArgument[5] { playerPosition.X, playerPosition.Y, playerPosition.Z, radius, 0 });
	}

	public static void ClearAreaOfEverything(Vector3 position, float radius)
	{
		Function.Call((Hash)0x957838AAF91BD12DuL, new InputArgument[8] { position.X, position.Y, position.Z, radius, false, false, false, false });
	}

	public static ParticleEffect CreateParticleEffectAtCoord(Vector3 coord, string name)
	{
		Function.Call((Hash)0x6C38AF3693A69A91uL, new InputArgument[1] { "core" });
		int handle = Function.Call<int>((Hash)0xE184F4F0DC5910E7uL, new InputArgument[12]
		{
			name, coord.X, coord.Y, coord.Z, 0.0, 0.0, 0.0, 1f, 0, 0,
			0, 0
		});
		return new ParticleEffect(handle);
	}
}
