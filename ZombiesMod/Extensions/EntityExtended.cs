using GTA;
using GTA.Native;

namespace ZombiesMod.Extensions;

public static class EntityExtended
{
	public static bool IsPlayingAnim(this Entity entity, string animSet, string animName)
	{
		return Function.Call<bool>((Hash)0x1F0B79228E461EC9uL, new InputArgument[4] { entity.Handle, animSet, animName, 3 });
	}

	public static void Fade(this Entity entity, bool state)
	{
		Function.Call((Hash)0x1F4ED342ACEFE62DuL, new InputArgument[2]
		{
			entity.Handle,
			state ? 1 : 0
		});
	}

	public static bool HasClearLineOfSight(this Entity entity, Entity target, float visionDistance)
	{
		return Function.Call<bool>((Hash)0x0267D00AF114F17AuL, new InputArgument[2] { entity.Handle, target.Handle }) && entity.Position.VDist(target.Position) < visionDistance;
	}
}
