using GTA.Math;

namespace ZombiesMod.Extensions;

public static class SystemExtended
{
	// Managed distance instead of GET_DISTANCE_BETWEEN_COORDS. This is called in the
	// per-tick zombie-AI loop for every nearby ped; avoiding the native round-trip each
	// call is a meaningful hot-path win for the same result.
	public static float VDist(this Vector3 v, Vector3 to)
	{
		return v.DistanceTo(to);
	}

	// Allocation-free squared distance for threshold comparisons (no sqrt).
	public static float VDistSquared(this Vector3 v, Vector3 to)
	{
		return v.DistanceToSquared(to);
	}
}
