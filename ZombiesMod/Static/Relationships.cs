using GTA;

namespace ZombiesMod.Static;

public class Relationships
{
	public static RelationshipGroup InfectedRelationship;

	public static RelationshipGroup FriendlyRelationship;

	public static RelationshipGroup MilitiaRelationship;

	public static RelationshipGroup HostileRelationship;

	public static RelationshipGroup PlayerRelationship;

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

	public static void SetRelationshipBothWays(Relationship rel, RelationshipGroup group1, RelationshipGroup group2)
	{
		group1.SetRelationshipBetweenGroups(group2, rel, bidirectionally: true);
	}
}
