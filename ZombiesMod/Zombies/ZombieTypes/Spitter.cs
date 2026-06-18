using GTA;
using ZombiesMod.Extensions;

namespace ZombiesMod.Zombies.ZombieTypes;

// Ranged zombie: instead of closing to melee it lobs corrosive spit at the target
// from a distance (throttled), making open ground dangerous.
public class Spitter : ZombiePed
{
	private readonly Ped _ped;

	private int _nextSpit;

	public override float AttackDistance => 18f;

	public override string MovementStyle { get; set; } = "move_m@injured";

	public Spitter(int handle)
		: base(handle)
	{
		_ped = this;
	}

	public override void OnAttackTarget(Ped target)
	{
		if (target.IsDead || Game.GameTime < _nextSpit)
		{
			return;
		}
		_nextSpit = Game.GameTime + 2500;
		_ped.Task.PlayAnimation("amb@world_human_bum_standing@drinking@idle_a", "idle_a", 8f, 1200, AnimationFlags.UpperBodyOnly);
		if (_ped.HasClearLineOfSight(target, 25f) && !target.IsInvincible)
		{
			target.ApplyDamage((int)((float)ZombiePed.ZombieDamage * 0.6f));
		}
	}

	public override void OnGoToTarget(Ped target)
	{
		_ped.Task.GoTo(target);
	}
}
