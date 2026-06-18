using GTA;
using GTA.Native;

namespace ZombiesMod.Extensions;

public static class GameExtended
{
	public static void DisableWeaponWheel()
	{
		Game.DisableControlThisFrame(2, Control.WeaponWheelLeftRight);
		Game.DisableControlThisFrame(2, Control.WeaponWheelNext);
		Game.DisableControlThisFrame(2, Control.WeaponWheelPrev);
		Game.DisableControlThisFrame(2, Control.WeaponWheelUpDown);
		Game.DisableControlThisFrame(2, Control.NextWeapon);
		Game.DisableControlThisFrame(2, Control.DropWeapon);
		Game.DisableControlThisFrame(2, Control.PrevWeapon);
		Game.DisableControlThisFrame(2, Control.WeaponSpecial);
		Game.DisableControlThisFrame(2, Control.WeaponSpecial2);
		Game.DisableControlThisFrame(2, Control.SelectWeapon);
	}

	public static int GetMobilePhoneId()
	{
		OutputArgument outputArgument = new OutputArgument();
		Function.Call(Hash._0xB4A53E05F68B6FA1, new InputArgument[1] { outputArgument });
		return outputArgument.GetResult<int>();
	}
}
