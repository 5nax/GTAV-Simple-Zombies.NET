using System;
using GTA.Native;
using ZombiesMod.Static;

namespace ZombiesMod.Extensions;

public static class UiExtended
{
	public static bool IsAnyHelpTextOnScreen()
	{
		return Function.Call<bool>((Hash)0x4D79439A6B55AC67uL, new InputArgument[0]);
	}

	public static void ClearAllHelpText()
	{
		Function.Call((Hash)0x6178F68A87A4D3A0uL, new InputArgument[0]);
	}

	public static void DisplayHelpTextThisFrame(string helpText, bool ignoreMenus = false)
	{
		if (ignoreMenus || !MenuConrtoller.MenuPool.AreAnyVisible)
		{
			Function.Call((Hash)0x8509B634FBE7DA11uL, new InputArgument[1] { "CELL_EMAIL_BCON" });
			for (int i = 0; i < helpText.Length; i += 99)
			{
				Function.Call((Hash)0x6C188BE134E074AAuL, new InputArgument[1] { helpText.Substring(i, Math.Min(99, helpText.Length - i)) });
			}
			Function.Call((Hash)0x238FFE5C7B0498A6uL, new InputArgument[4]
			{
				0,
				0,
				(!Function.Call<bool>((Hash)0x4D79439A6B55AC67uL, new InputArgument[0])) ? 1 : 0,
				-1
			});
		}
	}
}
