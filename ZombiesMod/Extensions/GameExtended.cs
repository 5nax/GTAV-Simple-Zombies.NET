using GTA;
using GTA.Native;

namespace ZombiesMod.Extensions;

public static class GameExtended
{
	public static void DisableWeaponWheel()
	{
		Game.DisableControlThisFrame(Control.WeaponWheelLeftRight);
		Game.DisableControlThisFrame(Control.WeaponWheelNext);
		Game.DisableControlThisFrame(Control.WeaponWheelPrev);
		Game.DisableControlThisFrame(Control.WeaponWheelUpDown);
		Game.DisableControlThisFrame(Control.NextWeapon);
		Game.DisableControlThisFrame(Control.DropWeapon);
		Game.DisableControlThisFrame(Control.PrevWeapon);
		Game.DisableControlThisFrame(Control.WeaponSpecial);
		Game.DisableControlThisFrame(Control.WeaponSpecial2);
		Game.DisableControlThisFrame(Control.SelectWeapon);
	}

	public static int GetMobilePhoneId()
	{
		OutputArgument outputArgument = new OutputArgument();
		Function.Call((Hash)0xB4A53E05F68B6FA1uL, new InputArgument[1] { outputArgument });
		return outputArgument.GetResult<int>();
	}
}
