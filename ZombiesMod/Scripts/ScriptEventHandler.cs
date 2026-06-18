using System;
using System.Collections.Generic;
using GTA;

namespace ZombiesMod.Scripts;

public class ScriptEventHandler : Script
{
	private readonly List<EventHandler> _wrapperEventHandlers;

	private readonly List<EventHandler> _scriptEventHandlers;

	private int _index;

	public static ScriptEventHandler Instance { get; private set; }

	public ScriptEventHandler()
	{
		Instance = this;
		_wrapperEventHandlers = new List<EventHandler>();
		_scriptEventHandlers = new List<EventHandler>();
		base.Tick += OnTick;
	}

	public void RegisterScript(EventHandler eventHandler)
	{
		_scriptEventHandlers.Add(eventHandler);
	}

	public void UnregisterScript(EventHandler eventHandler)
	{
		_scriptEventHandlers.Remove(eventHandler);
	}

	public void RegisterWrapper(EventHandler eventHandler)
	{
		_wrapperEventHandlers.Add(eventHandler);
	}

	public void UnregisterWrapper(EventHandler eventHandler)
	{
		_wrapperEventHandlers.Remove(eventHandler);
	}

	private void OnTick(object sender, EventArgs eventArgs)
	{
		UpdateWrappers(sender, eventArgs);
		UpdateScripts(sender, eventArgs);
	}

	private void UpdateScripts(object sender, EventArgs eventArgs)
	{
		// Snapshot so a handler that registers/unregisters another mid-invoke
		// cannot corrupt the iteration.
		EventHandler[] handlers = _scriptEventHandlers.ToArray();
		for (int num = handlers.Length - 1; num >= 0; num--)
		{
			handlers[num]?.Invoke(sender, eventArgs);
		}
	}

	private void UpdateWrappers(object sender, EventArgs eventArgs)
	{
		// Snapshot the list for a stable window; the original indexed the live list,
		// so register/unregister during a cycle could skip or replay handlers.
		EventHandler[] handlers = _wrapperEventHandlers.ToArray();
		if (handlers.Length == 0)
		{
			_index = 0;
			return;
		}
		if (_index >= handlers.Length)
		{
			_index = 0;
		}
		int end = System.Math.Min(_index + 5, handlers.Length);
		for (int i = _index; i < end; i++)
		{
			handlers[i]?.Invoke(sender, eventArgs);
		}
		_index = (end >= handlers.Length) ? 0 : end;
	}
}
