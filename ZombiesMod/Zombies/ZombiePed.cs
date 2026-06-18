using System;
using System.Linq;
using GTA;
using GTA.Math;
using ZombiesMod.Extensions;
using ZombiesMod.Scripts;
using ZombiesMod.Static;
using ZombiesMod.Wrappers;

namespace ZombiesMod.Zombies;

// Composition over inheritance: SHVDN v3 makes GTA entity constructors internal, so a
// mod assembly can no longer subclass Entity/Ped. ZombiePed now wraps a Ped and delegates
// the entity surface it needs; the implicit ZombiePed->Ped operator keeps call sites terse.
public abstract class ZombiePed : IEquatable<Ped>
{
	public delegate void OnGoingToTargetEvent(Ped target);

	public delegate void OnAttackingTargetEvent(Ped target);

	public const int MovementUpdateInterval = 5;

	public static int ZombieDamage = 15;

	public static float SensingRange = 120f;

	public static float SilencerEffectiveRange = 15f;

	public static float BehindZombieNoticeDistance = 5f;

	public static float RunningNoticeDistance = 25f;

	public static float AttackRange = 1.2f;

	public static float VisionDistance = 35f;

	public static float WanderRadius = 100f;

	private Ped _target;

	private readonly Ped _ped;

	private EntityEventWrapper _eventWrapper;

	private bool _goingToTarget;

	private bool _attackingTarget;

	private DateTime _currentMovementUpdateTime;

	public virtual bool PlayAudio { get; set; }

	public Ped Target
	{
		get
		{
			return _target;
		}
		private set
		{
			if (value == null && _target != null)
			{
				_ped.Task.WanderAround(Position, WanderRadius);
				bool goingToTarget = (AttackingTarget = false);
				GoingToTarget = goingToTarget;
			}
			_target = value;
		}
	}

	public bool GoingToTarget
	{
		get
		{
			return _goingToTarget;
		}
		set
		{
			if (value && !_goingToTarget)
			{
				this.GoToTarget?.Invoke(Target);
			}
			_goingToTarget = value;
		}
	}

	public bool AttackingTarget
	{
		get
		{
			return _attackingTarget;
		}
		set
		{
			if (value && !_ped.IsRagdoll && !_ped.IsDead && !_ped.IsClimbing && !_ped.IsFalling && !_ped.IsBeingStunned && !_ped.IsGettingUp)
			{
				this.AttackTarget?.Invoke(Target);
			}
			_attackingTarget = value;
		}
	}

	public virtual string MovementStyle { get; set; }

	public event OnGoingToTargetEvent GoToTarget;

	public event OnAttackingTargetEvent AttackTarget;

	// Delegated entity surface (was inherited from Entity in v2).
	public Vector3 Position => _ped.Position;

	public int MaxHealth => _ped.MaxHealth;

	public bool Exists() => _ped != null && _ped.Exists();

	public void Delete() => _ped?.Delete();

	protected ZombiePed(int handle)
	{
		_ped = (Ped)Entity.FromHandle(handle);
		_eventWrapper = new EntityEventWrapper(_ped);
		_eventWrapper.Died += OnDied;
		_eventWrapper.Updated += Update;
		_eventWrapper.Aborted += Abort;
		_currentMovementUpdateTime = DateTime.UtcNow;
		GoToTarget += OnGoToTarget;
		AttackTarget += OnAttackTarget;
	}

	public abstract void OnAttackTarget(Ped target);

	public abstract void OnGoToTarget(Ped target);

	private void OnDied(EntityEventWrapper sender, Entity entity)
	{
		_ped.AttachedBlip?.Delete();
		if (ZombieVehicleSpawner.Instance.IsInvalidZone(entity.Position) && ZombieVehicleSpawner.Instance.IsValidSpawn(entity.Position))
		{
			ZombieVehicleSpawner.Instance.SpawnBlocker.Add(entity.Position);
		}
	}

	public void Update(EntityEventWrapper entityEventWrapper, Entity entity)
	{
		if (Position.VDist(Database.PlayerPosition) > 120f && (!_ped.IsOnScreen || _ped.IsDead))
		{
			Delete();
		}
		if (PlayAudio && _ped.IsRunning)
		{
			_ped.DisablePainAudio(toggle: false);
			_ped.PlayPain(8);
			_ped.PlayFacialAnim("facials@gen_male@base", "burning_1");
		}
		GetTarget();
		SetWalkStyle();
		if (_ped.IsOnFire && !_ped.IsDead)
		{
			_ped.Kill();
		}
		_ped.StopAmbientSpeechThisFrame();
		if (!PlayAudio)
		{
			_ped.StopSpeaking(shaking: true);
		}
		if (!(Target == null))
		{
			float num = Position.VDist(Target.Position);
			if (num > AttackRange)
			{
				AttackingTarget = false;
				GoingToTarget = true;
			}
			else
			{
				AttackingTarget = true;
				GoingToTarget = false;
			}
		}
	}

	public void Abort(EntityEventWrapper sender, Entity entity)
	{
		Delete();
	}

	public void InfectTarget(Ped target)
	{
		if (!target.IsPlayer && target.Health <= target.MaxHealth / 4)
		{
			target.SetToRagdoll(3000);
			ZombieCreator.InfectPed(target, MaxHealth, overrideAsFastZombie: true);
			ForgetTarget();
			target.LeaveGroup();
			target.Weapons.Drop();
			EntityEventWrapper.Dispose(target);
		}
	}

	public void ForgetTarget()
	{
		_target = null;
	}

	private void SetWalkStyle()
	{
		if (!(DateTime.UtcNow <= _currentMovementUpdateTime))
		{
			_ped.SetMovementAnimSet(MovementStyle);
			UpdateTime();
		}
	}

	private void UpdateTime()
	{
		_currentMovementUpdateTime = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
	}

	private void GetTarget()
	{
		Ped[] spatials = World.GetNearbyPeds(_ped, SensingRange).Where(IsGoodTarget).ToArray();
		Ped closest = World.GetClosest(Position, spatials);
		if (closest != null && (_ped.HasClearLineOfSight(closest, VisionDistance) || CanHearPed(closest)))
		{
			Target = closest;
		}
		else if ((Target != null && !IsGoodTarget(Target)) || closest != Target)
		{
			Target = null;
		}
	}

	private bool CanHearPed(Ped ped)
	{
		float distance = ped.Position.VDist(Position);
		return !IsWeaponWellSilenced(ped, distance) || IsBehindZombie(distance) || IsRunningNoticed(ped, distance);
	}

	private static bool IsRunningNoticed(Ped ped, float distance)
	{
		return ped.IsSprinting && distance < RunningNoticeDistance;
	}

	private static bool IsBehindZombie(float distance)
	{
		return distance < BehindZombieNoticeDistance;
	}

	private static bool IsWeaponWellSilenced(Ped ped, float distance)
	{
		if (!ped.IsShooting)
		{
			return true;
		}
		return ped.IsCurrentWeaponSileced() && distance > SilencerEffectiveRange;
	}

	private bool IsGoodTarget(Ped ped)
	{
		return ped.GetRelationshipWithPed(_ped) == Relationship.Hate;
	}

	protected bool Equals(ZombiePed other)
	{
		return object.Equals(_ped, other._ped);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		return obj.GetType() == GetType() && Equals((ZombiePed)obj);
	}

	public override int GetHashCode()
	{
		return (base.GetHashCode() * 397) ^ ((_ped != null) ? _ped.GetHashCode() : 0);
	}

	public bool Equals(Ped other)
	{
		return object.Equals(_ped, other);
	}

	public static implicit operator Ped(ZombiePed v)
	{
		return v._ped;
	}
}
