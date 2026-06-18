using GTA;
using GTA.Math;
using ZombiesMod.Extensions;
using ZombiesMod.Scripts;
using ZombiesMod.Static;

namespace ZombiesMod.DataClasses;

public abstract class Survivors
{
	public delegate void SurvivorCompletedEvent(Survivors survivors);

	public static float MaxSpawnDistance = 650f;

	public static float MinSpawnDistance = 400f;

	public static float DeleteRange = 1000f;

	public Ped PlayerPed => Database.PlayerPed;

	public Vector3 PlayerPosition => Database.PlayerPosition;

	public virtual event SurvivorCompletedEvent Completed;

	public abstract void Update();

	public abstract void SpawnEntities();

	public abstract void CleanUp();

	public abstract void Abort();

	public void Complete()
	{
		this.Completed?.Invoke(this);
	}

	public bool IsValidSpawn(Vector3 spawn)
	{
		if (!(spawn.VDist(PlayerPosition) < MinSpawnDistance) && !ZombieVehicleSpawner.Instance.IsInvalidZone(spawn))
		{
			return true;
		}
		Complete();
		return false;
	}

	public Vector3 GetSpawnPoint()
	{
		Vector3 position = PlayerPosition.Around(MaxSpawnDistance);
		return World.GetNextPositionOnStreet(position);
	}
}
