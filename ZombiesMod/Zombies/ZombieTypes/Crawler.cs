using GTA;
using ZombiesMod.Extensions;

namespace ZombiesMod.Zombies.ZombieTypes;

// Low, fast, fragile zombie that scuttles in a crouched gait. Easy to kill but quick.
public class Crawler : ZombiePed
{
	private readonly Ped _ped;

	public override string MovementStyle { get; set; } = "move_ped_crouched";

	public Crawler(int handle)
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
			_ped.Task.PlayAnimation("rcmbarry", "bar_1_teleport_aln", 8f, 800, AnimationFlags.UpperBodyOnly);
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
}
