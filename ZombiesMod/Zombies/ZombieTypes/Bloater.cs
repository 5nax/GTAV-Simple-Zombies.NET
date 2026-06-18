using GTA;
using ZombiesMod.Extensions;

namespace ZombiesMod.Zombies.ZombieTypes;

// Bloated zombie that bursts into a fiery toxic explosion when killed — punishes
// fighting it in melee or in a crowd.
public class Bloater : ZombiePed
{
	private readonly Ped _ped;

	public override string MovementStyle { get; set; } = "move_m@drunk@verydrunk";

	public Bloater(int handle)
		: base(handle)
	{
		_ped = this;
	}

	public override void OnAttackTarget(Ped target)
	{
		if (target.IsDead)
		{
			return;
		}
		if (!_ped.IsPlayingAnim("rcmbarry", "bar_1_teleport_aln"))
		{
			_ped.Task.PlayAnimation("rcmbarry", "bar_1_teleport_aln", 8f, 1000, AnimationFlags.UpperBodyOnly);
		}
		if (!target.IsInvincible)
		{
			target.ApplyDamage(ZombiePed.ZombieDamage);
		}
		InfectTarget(target);
	}

	public override void OnGoToTarget(Ped target)
	{
		_ped.Task.GoTo(target);
	}

	protected override void OnZombieDied()
	{
		World.AddExplosion(Position, ExplosionType.Molotov1, 6f, 0.5f);
	}
}
