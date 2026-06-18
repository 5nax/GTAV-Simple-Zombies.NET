using GTA;
using GTA.Native;

namespace ZombiesMod.Extensions;

public static class PlayerExtended
{
	public static void IgnoreLowPriorityShockingEvents(this Player player, bool toggle)
	{
		Function.Call(Hash._0x596976B02B6B5700, new InputArgument[2]
		{
			player.Handle,
			toggle ? 1 : 0
		});
	}
}
