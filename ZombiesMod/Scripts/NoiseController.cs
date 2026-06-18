using System;
using GTA;
using GTA.Math;
using ZombiesMod.Extensions;
using ZombiesMod.Static;

namespace ZombiesMod.Scripts;

// Stealth & sound. Unsuppressed gunfire carries far and pulls every nearby walker
// toward the shot; sprinting makes a smaller racket; suppressors and moving quietly
// keep you hidden. This is what makes a firefight a last resort instead of a plan.
public class NoiseController : Script
{
	private int _nextBroadcast;

	private static Ped PlayerPed => Database.PlayerPed;

	public NoiseController()
	{
		Tick += OnTick;
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!GameConfig.StealthEnabled || Database.PlayerIsDead
			|| ZombieVehicleSpawner.Instance == null || !ZombieVehicleSpawner.Instance.Spawn)
		{
			return;
		}
		if (Game.GameTime < _nextBroadcast)
		{
			return;
		}
		Ped player = PlayerPed;
		if (player == null || !player.Exists())
		{
			return;
		}
		float radius = CurrentNoiseRadius(player);
		if (radius <= 0f)
		{
			return;
		}
		_nextBroadcast = Game.GameTime + 1200;
		LureNearby(player.Position, radius);
	}

	private static float CurrentNoiseRadius(Ped player)
	{
		if (player.IsShooting)
		{
			return player.IsCurrentWeaponSileced() ? GameConfig.SuppressedNoiseRadius : GameConfig.GunshotNoiseRadius;
		}
		if (Database.PlayerIsSprinting)
		{
			return GameConfig.SprintNoiseRadius;
		}
		return 0f;
	}

	// Pull idle dead within earshot toward the sound; the per-zombie AI takes over
	// (acquires the player by sight/sound) once they shamble close.
	private void LureNearby(Vector3 source, float radius)
	{
		Ped[] heard = World.GetNearbyPeds(PlayerPed, radius);
		foreach (Ped z in heard)
		{
			if (z == null || !z.Exists() || z.IsDead || z.IsPlayer)
			{
				continue;
			}
			if (z.RelationshipGroup != Relationships.InfectedRelationship)
			{
				continue;
			}
			if (z.IsInCombatAgainst(PlayerPed))
			{
				continue; // already hunting you
			}
			z.Task.GoTo(source, 10000);
		}
	}
}
