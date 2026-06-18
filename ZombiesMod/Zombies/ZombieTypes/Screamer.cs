using System;
using GTA;
using GTA.Math;
using ZombiesMod.Extensions;

namespace ZombiesMod.Zombies.ZombieTypes;

// On first contact it shrieks, alerting the horde. HordeController listens to the
// Screamed event and summons reinforcements toward the scream position.
public class Screamer : ZombiePed
{
	private readonly Ped _ped;

	private bool _screamed;

	public override bool PlayAudio { get; set; } = true;

	public override string MovementStyle { get; set; } = "move_m@injured";

	// Raised at the scream position; HordeController subscribes to spawn reinforcements.
	public static event Action<Vector3> Screamed;

	public Screamer(int handle)
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
		if (!_screamed)
		{
			_screamed = true;
			_ped.Task.PlayAnimation("mp_player_int_upperface", "mp_player_int_face", 8f, 2000, AnimationFlags.UpperBodyOnly);
			Notifier.Show("~r~A screamer cries out for the horde!~s~", blinking: true);
			Screamer.Screamed?.Invoke(Position);
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
}
