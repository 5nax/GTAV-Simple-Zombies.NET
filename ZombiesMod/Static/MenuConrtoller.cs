using System;
using GTA;
using LemonUI;
using LemonUI.TimerBars;

namespace ZombiesMod.Static;

// Hosts the single LemonUI ObjectPool (menus) and TimerBarCollection (HUD bars) and
// pumps them every frame. (Class name keeps the original "Conrtoller" spelling so
// existing references and any serialized data stay valid.)
public class MenuConrtoller : Script
{
	private static ObjectPool _menuPool;

	private static TimerBarCollection _barPool;

	public static ObjectPool MenuPool => _menuPool ??= new ObjectPool();

	public static TimerBarCollection BarPool => _barPool ??= new TimerBarCollection();

	public MenuConrtoller()
	{
		Tick += OnTick;
	}

	public void OnTick(object sender, EventArgs eventArgs)
	{
		// Draw HUD timer bars only when no menu is open (the menu takes visual priority).
		if (_barPool != null && (_menuPool == null || !_menuPool.AreAnyVisible))
		{
			_barPool.Process();
		}
		_menuPool?.Process();
	}
}
