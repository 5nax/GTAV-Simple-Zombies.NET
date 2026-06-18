using System;
using GTA;
using GTA.Math;
using ZombiesMod.Extensions;
using ZombiesMod.Static;
using ZombiesMod.SurvivorTypes;

namespace ZombiesMod.Scripts;

public class RecruitPeds : Script
{
	public const float InteractDistance = 1.5f;

	private static Ped PlayerPed => Database.PlayerPed;

	private static Vector3 PlayerPosition => Database.PlayerPosition;

	public RecruitPeds()
	{
		base.Tick += OnTick;
	}

	private static void OnTick(object sender, EventArgs eventArgs)
	{
		if (MenuConrtoller.MenuPool.AreAnyVisible || PlayerPed.PedGroup.MemberCount >= 6)
		{
			return;
		}
		Ped[] nearbyPeds = World.GetNearbyPeds(PlayerPed, 1.5f);
		Ped closest = World.GetClosest(PlayerPosition, nearbyPeds);
		if (closest == null || closest.IsDead || closest.IsInCombatAgainst(PlayerPed) || closest.GetRelationshipWithPed(PlayerPed) == Relationship.Hate || closest.RelationshipGroup != Relationships.FriendlyRelationship || closest.PedGroup == PlayerPed.PedGroup)
		{
			return;
		}
		Game.DisableControlThisFrame(Control.Enter);
		UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_ENTER~ to recruit this ped.");
		if (Ctrl.DisabledJustPressed(Control.Enter))
		{
			if (FriendlySurvivors.Instance != null)
			{
				FriendlySurvivors.Instance.RemovePed(closest);
			}
			closest.Recruit(PlayerPed);
			if (PlayerPed.PedGroup.MemberCount >= 6)
			{
				Notifier.Show("You've reached the max amount of ~b~guards~s~.");
			}
		}
	}
}
