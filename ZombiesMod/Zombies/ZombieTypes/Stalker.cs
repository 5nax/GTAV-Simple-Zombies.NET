using GTA;
using ZombiesMod.Extensions;

namespace ZombiesMod.Zombies.ZombieTypes;

// Blind "clicker"-style zombie that hunts by sound alone (CanSee = false). Sneak past
// it and it never notices; fire a gun or sprint nearby and it comes straight for you.
public class Stalker : ZombiePed
{
	private readonly Ped _ped;

	public override bool PlayAudio { get; set; } = true;

	public override string MovementStyle { get; set; } = "move_m@injured";

	protected override bool CanSee => false;

	public Stalker(int handle)
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
			_ped.Task.PlayAnimation("rcmbarry", "bar_1_teleport_aln", 8f, 900, AnimationFlags.UpperBodyOnly);
		}
		if (!target.IsInvincible)
		{
			target.ApplyDamage((int)((float)ZombiePed.ZombieDamage * 1.5f));
		}
		InfectTarget(target);
	}

	public override void OnGoToTarget(Ped target)
	{
		_ped.Task.GoTo(target);
	}
}
