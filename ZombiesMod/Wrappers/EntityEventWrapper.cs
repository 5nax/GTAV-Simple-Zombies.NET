using System;
using System.Collections.Generic;
using GTA;
using ZombiesMod.Scripts;

namespace ZombiesMod.Wrappers;

public class EntityEventWrapper
{
	public delegate void OnDeathEvent(EntityEventWrapper sender, Entity entity);

	public delegate void OnWrapperAbortedEvent(EntityEventWrapper sender, Entity entity);

	public delegate void OnWrapperUpdateEvent(EntityEventWrapper sender, Entity entity);

	public delegate void OnWrapperDisposedEvent(EntityEventWrapper sender, Entity entity);

	private static readonly List<EntityEventWrapper> Wrappers = new List<EntityEventWrapper>();

	private bool _isDead;

	private readonly EventHandler _onScriptAborted;

	public Entity Entity { get; }

	public bool IsDead
	{
		get
		{
			return Entity.IsDead;
		}
		private set
		{
			if (value && !_isDead)
			{
				this.Died?.Invoke(this, Entity);
			}
			_isDead = value;
		}
	}

	public event OnDeathEvent Died;

	public event OnWrapperAbortedEvent Aborted;

	public event OnWrapperUpdateEvent Updated;

	public event OnWrapperDisposedEvent Disposed;

	public EntityEventWrapper(Entity ent)
	{
		Entity = ent;
		ScriptEventHandler.Instance.RegisterWrapper(OnTick);
		// Keep a reference so Dispose can detach it (the original subscribed an
		// anonymous delegate it could never remove -> wrapper retained for the
		// script's lifetime).
		_onScriptAborted = delegate
		{
			Abort();
		};
		ScriptEventHandler.Instance.Aborted += _onScriptAborted;
		Wrappers.Add(this);
	}

	public void OnTick(object sender, EventArgs eventArgs)
	{
		if (Entity == null || !Entity.Exists())
		{
			Dispose();
			return;
		}
		IsDead = Entity.IsDead;
		this.Updated?.Invoke(this, Entity);
	}

	public void Abort()
	{
		this.Aborted?.Invoke(this, Entity);
	}

	public void Dispose()
	{
		ScriptEventHandler.Instance.UnregisterWrapper(OnTick);
		if (ScriptEventHandler.Instance != null && _onScriptAborted != null)
		{
			ScriptEventHandler.Instance.Aborted -= _onScriptAborted;
		}
		Wrappers.Remove(this);
		this.Disposed?.Invoke(this, Entity);
	}

	public static void Dispose(Entity entity)
	{
		EntityEventWrapper entityEventWrapper = Wrappers.Find((EntityEventWrapper w) => w.Entity == entity);
		// Dispose() already removes it from Wrappers; no redundant second Remove.
		entityEventWrapper?.Dispose();
	}
}
