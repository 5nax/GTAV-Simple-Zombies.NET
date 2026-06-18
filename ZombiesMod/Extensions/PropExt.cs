using GTA;
using GTA.Native;

namespace ZombiesMod.Extensions;

public static class PropExt
{
	public static void SetStateOfDoor(this Prop prop, bool locked, DoorState heading)
	{
		Function.Call((Hash)0xF82D8F1926A02C3DuL, new InputArgument[7]
		{
			prop.Model.Hash,
			prop.Position.X,
			prop.Position.Y,
			prop.Position.Z,
			locked,
			(int)heading,
			1
		});
	}

	public unsafe static bool GetDoorLockState(this Prop prop)
	{
		bool result = false;
		int num = 0;
		Function.Call((Hash)0xEDC1A5B84AEF33FFuL, new InputArgument[6]
		{
			prop.Model.Hash,
			prop.Position.X,
			prop.Position.Y,
			prop.Position.Z,
			&result,
			&num
		});
		return result;
	}
}
