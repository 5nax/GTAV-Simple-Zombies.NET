using System;
using System.Linq;
using GTA;
using ZombiesMod.Extensions;
using ZombiesMod.Static;

namespace ZombiesMod.Scripts;

// Keeps the world tidy and the frame-rate up by clearing dead infected bodies once
// they pile up. Only touches zombies (Infected relationship), never the player's group
// or live peds, and prefers the farthest corpses so nearby carnage stays visible.
public class CorpseCleanup : Script
{
	public CorpseCleanup()
	{
		Interval = 4000;
		Tick += OnTick;
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!GameConfig.CorpseCleanupEnabled)
		{
			return;
		}
		Ped[] corpses = World.GetAllPeds()
			.Where((Ped p) => p != null && p.Exists() && p.IsDead && !p.IsPlayer
				&& p.RelationshipGroup == Relationships.InfectedRelationship)
			.OrderByDescending((Ped p) => p.Position.VDist(Database.PlayerPosition))
			.ToArray();

		int over = corpses.Length - GameConfig.MaxCorpses;
		for (int i = 0; i < over; i++)
		{
			Ped corpse = corpses[i];
			// Don't delete a corpse the player is standing over.
			if (corpse.Position.VDist(Database.PlayerPosition) > 25f)
			{
				corpse.MarkAsNoLongerNeeded();
				corpse.Delete();
			}
		}
	}
}
