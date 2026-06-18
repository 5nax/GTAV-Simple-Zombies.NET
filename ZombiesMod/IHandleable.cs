namespace ZombiesMod;

// SHVDN v2 exposed GTA.IHandleable; SHVDN v3 removed it. The mod only uses it as a
// contract for its own data classes (MapProp, VehicleData, PedData, ParticleEffect),
// all of which expose an int Handle, so we re-declare it locally.
public interface IHandleable
{
	int Handle { get; }
}
