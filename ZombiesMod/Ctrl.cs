using GTA;
using GTA.Native;

// Global-namespace input helper. SHVDN v3 dropped the player-index argument from the
// control helpers and no longer exposes IsDisabledControlJustPressed, so we call the
// underlying native (IS_DISABLED_CONTROL_JUST_PRESSED) directly.
public static class Ctrl
{
	public static bool DisabledJustPressed(Control control)
	{
		return Function.Call<bool>((Hash)0x91AEF906BCA88877uL, 2, (int)control);
	}

	public static bool DisabledPressed(Control control)
	{
		return Function.Call<bool>((Hash)0xE2587F8CBBD87B1DuL, 2, (int)control);
	}
}
