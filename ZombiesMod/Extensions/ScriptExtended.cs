using GTA.Native;

namespace ZombiesMod.Extensions;

public static class ScriptExtended
{
	public static void TerminateScriptByName(string name)
	{
		if (Function.Call<bool>(Hash._0xFC04745FBE67C19A, new InputArgument[1] { name }))
		{
			Function.Call(Hash._0x9DC711BC69C548DF, new InputArgument[1] { name });
		}
	}
}
