using GTA;
using ZombiesMod.Extensions;

namespace ZombiesMod.Zombies.ZombieTypes;

// Tanky, slow zombie that hits hard and knocks targets down. Health is boosted by
// ZombieCreator when it picks this variant.
public class Brute : ZombiePed
{
	private readonly Ped _ped;

	public override bool PlayAudio { get; set; } = true;

	public override string MovementStyle { get; set; } = "move_m@drunk@verydrunk";

	public Brute(int handle)
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
			_ped.Task.PlayAnimation("rcmbarry", "bar_1_teleport_aln", 8f, 1200, AnimationFlags.UpperBodyOnly);
		}
		if (!target.IsInvincible)
		{
			target.ApplyDamage(ZombiePed.ZombieDamage * 3);
			target.SetToRagdoll(1500);
		}
		InfectTarget(target);
	}

	public override void OnGoToTarget(Ped target)
	{
		_ped.Task.GoTo(target);
	}
}
