using GTA.Native;

namespace ZombiesMod.Extensions;

public static class ScriptExtended
{
	public static void TerminateScriptByName(string name)
	{
		if (Function.Call<bool>((Hash)0xFC04745FBE67C19AuL, new InputArgument[1] { name }))
		{
			Function.Call((Hash)0x9DC711BC69C548DFuL, new InputArgument[1] { name });
		}
	}
}
