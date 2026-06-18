using GTA;
using GTA.Native;

namespace ZombiesMod.Extensions;

public static class VehicleExtended
{
	public static VehicleClass GetModelClass(Model vehicleModel)
	{
		return (VehicleClass)Function.Call<int>((Hash)0xDEDF1C8BD47C2200uL, new InputArgument[1] { vehicleModel.Hash });
	}
}
