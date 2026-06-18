using GTA;
using GTA.Native;
using ZombiesMod.Extensions;
using ZombiesMod.Static;

namespace ZombiesMod.Zombies.ZombieTypes;

public class Runner : ZombiePed
{
	private readonly Ped _ped;

	private bool _jumpAttack;

	public override bool PlayAudio { get; set; } = true;

	public override string MovementStyle { get; set; } = "move_m@injured";

	public Runner(int handle)
		: base(handle)
	{
		_ped = this;
	}

	public override void OnAttackTarget(Ped target)
	{
		if (target.IsDead)
		{
			if (!_ped.IsPlayingAnim("amb@world_human_bum_wash@male@high@idle_a", "idle_b"))
			{
				_ped.Task.PlayAnimation("amb@world_human_bum_wash@male@high@idle_a", "idle_b", 8f, -1, AnimationFlags.Loop);
			}
		}
		else if (Database.Random.NextDouble() < 0.30000001192092896 && !_jumpAttack && !target.IsPerformingStealthKill && !target.IsGettingUp && !target.IsRagdoll)
		{
			_ped.Jump();
			_ped.Heading = (target.Position - Position).ToHeading();
			_jumpAttack = true;
			target.SetToRagdoll(2000);
		}
		else if (!_ped.IsPlayingAnim("rcmbarry", "bar_1_teleport_aln"))
		{
			_ped.Task.PlayAnimation("rcmbarry", "bar_1_teleport_aln", 8f, 1000, AnimationFlags.UpperBodyOnly);
			if (!target.IsInvincible)
			{
				target.ApplyDamage(ZombiePed.ZombieDamage);
			}
			InfectTarget(target);
		}
	}

	public override void OnGoToTarget(Ped target)
	{
		Function.Call(Hash._0x6A071245EB0D1882, new InputArgument[7] { _ped.Handle, target.Handle, -1, 0f, 5f, 1073741824, 0 });
	}
}
