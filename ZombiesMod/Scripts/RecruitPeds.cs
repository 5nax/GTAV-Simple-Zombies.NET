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
		if (MenuConrtoller.MenuPool.IsAnyMenuOpen() || PlayerPed.CurrentPedGroup.MemberCount >= 6)
		{
			return;
		}
		Ped[] nearbyPeds = World.GetNearbyPeds(PlayerPed, 1.5f);
		Ped closest = World.GetClosest(PlayerPosition, nearbyPeds);
		if (closest == null || closest.IsDead || closest.IsInCombatAgainst(PlayerPed) || closest.GetRelationshipWithPed(PlayerPed) == Relationship.Hate || closest.RelationshipGroup != Relationships.FriendlyRelationship || closest.CurrentPedGroup == PlayerPed.CurrentPedGroup)
		{
			return;
		}
		Game.DisableControlThisFrame(2, Control.Enter);
		UiExtended.DisplayHelpTextThisFrame("Press ~INPUT_ENTER~ to recruit this ped.");
		if (Game.IsDisabledControlJustPressed(2, Control.Enter))
		{
			if (FriendlySurvivors.Instance != null)
			{
				FriendlySurvivors.Instance.RemovePed(closest);
			}
			closest.Recruit(PlayerPed);
			if (PlayerPed.CurrentPedGroup.MemberCount >= 6)
			{
				UI.Notify("You've reached the max amount of ~b~guards~s~.");
			}
		}
	}
}
