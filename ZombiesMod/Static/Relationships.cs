using GTA;

namespace ZombiesMod.Static;

public class Relationships
{
	public static int InfectedRelationship;

	public static int FriendlyRelationship;

	public static int MilitiaRelationship;

	public static int HostileRelationship;

	public static int PlayerRelationship;

	static Relationships()
	{
	}

	public static void SetRelationships()
	{
		InfectedRelationship = World.AddRelationshipGroup("Zombie");
		FriendlyRelationship = World.AddRelationshipGroup("Friendly");
		MilitiaRelationship = World.AddRelationshipGroup("Private_Militia");
		HostileRelationship = World.AddRelationshipGroup("Hostile");
		PlayerRelationship = Database.PlayerPed.RelationshipGroup;
		SetRelationshipBothWays(Relationship.Hate, InfectedRelationship, FriendlyRelationship);
		SetRelationshipBothWays(Relationship.Hate, InfectedRelationship, MilitiaRelationship);
		SetRelationshipBothWays(Relationship.Hate, InfectedRelationship, HostileRelationship);
		SetRelationshipBothWays(Relationship.Hate, InfectedRelationship, PlayerRelationship);
		SetRelationshipBothWays(Relationship.Hate, FriendlyRelationship, MilitiaRelationship);
		SetRelationshipBothWays(Relationship.Hate, FriendlyRelationship, HostileRelationship);
		SetRelationshipBothWays(Relationship.Hate, HostileRelationship, MilitiaRelationship);
		SetRelationshipBothWays(Relationship.Hate, HostileRelationship, PlayerRelationship);
		SetRelationshipBothWays(Relationship.Hate, PlayerRelationship, MilitiaRelationship);
		SetRelationshipBothWays(Relationship.Like, PlayerRelationship, FriendlyRelationship);
		Database.PlayerPed.IsPriorityTargetForEnemies = true;
	}

	public static void SetRelationshipBothWays(Relationship rel, int group1, int group2)
	{
		World.SetRelationshipBetweenGroups(rel, group1, group2);
		World.SetRelationshipBetweenGroups(rel, group2, group1);
	}
}
