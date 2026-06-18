using System;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.UI;
using ZombiesMod.Static;
using Font = GTA.UI.Font;

namespace ZombiesMod.PlayerManagement;

// Injury system: a zombie hit can open a bleeding wound that steadily drains health
// until you patch it with a Bandage/Medkit (ItemEvent.StopBleeding). Raises the cost
// of melee exchanges and makes medical supplies matter.
public class PlayerBleeding : Script
{
	public static PlayerBleeding Instance { get; private set; }

	public static bool IsBleeding { get; private set; }

	private int _nextHitCheck;

	private float _bleedTickAccumulator;

	private static Ped PlayerPed => Database.PlayerPed;

	public PlayerBleeding()
	{
		Instance = this;
		Tick += OnTick;
	}

	public void Stop()
	{
		if (IsBleeding)
		{
			IsBleeding = false;
			Notifier.Show("~g~Wound bandaged~s~ — bleeding stopped.");
		}
	}

	private void OnTick(object sender, EventArgs e)
	{
		if (!GameConfig.BleedingEnabled || Database.PlayerIsDead)
		{
			IsBleeding = false;
			return;
		}

		DetectWound();

		if (IsBleeding)
		{
			_bleedTickAccumulator += Game.LastFrameTime * GameConfig.BleedDamagePerSecond;
			if (_bleedTickAccumulator >= 1f)
			{
				int dmg = (int)_bleedTickAccumulator;
				_bleedTickAccumulator -= dmg;
				PlayerPed.Health -= dmg;
			}
			DrawWarning();
		}
	}

	private void DetectWound()
	{
		Ped player = PlayerPed;
		if (player == null || !player.Exists() || Game.GameTime < _nextHitCheck)
		{
			return;
		}
		Ped[] nearby = World.GetNearbyPeds(player, 3f);
		bool struck = nearby.Any((Ped p) =>
			p != null && p.Exists() && !p.IsPlayer
			&& p.RelationshipGroup == Relationships.InfectedRelationship
			&& player.HasBeenDamagedBy(p));
		if (!struck)
		{
			return;
		}
		_nextHitCheck = Game.GameTime + 1500;
		if (!IsBleeding && Database.Random.Next(100) < GameConfig.BleedChancePercent)
		{
			IsBleeding = true;
			Notifier.Show("~r~You're bleeding!~s~ Use a Bandage before you bleed out.", blinking: true);
		}
	}

	private void DrawWarning()
	{
		if (MenuConrtoller.MenuPool.AreAnyVisible)
		{
			return;
		}
		// Pulse so it reads as urgent.
		bool on = Game.GameTime % 1000 < 600;
		if (on)
		{
			new TextElement("~r~BLEEDING", new PointF(15f, 52f), 0.4f, Color.IndianRed, Font.ChaletLondon, Alignment.Left, shadow: true, outline: false).Draw();
		}
	}
}
