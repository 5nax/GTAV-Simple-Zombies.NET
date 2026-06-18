using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using GTA;
using LemonUI.Menus;
using ZombiesMod.PlayerManagement;
using ZombiesMod.Static;

namespace ZombiesMod;

// A dedicated, categorized crafting workshop (opened with the crafting key, default K).
// Recipes are grouped by type, each entry shows its component cost with have/need
// colouring and a craftable tick, and activating an entry crafts it and refreshes.
public class CraftingMenu : Script
{
	private static readonly string[] Categories = { "Medical & Gear", "Weapons", "Building", "Food", "Tools" };

	private readonly NativeMenu _root;

	private readonly Dictionary<string, NativeMenu> _categoryMenus = new Dictionary<string, NativeMenu>();

	private readonly Keys _craftKey;

	public CraftingMenu()
	{
		_craftKey = Settings.GetValue("keys", "crafting_key", Keys.K);
		Settings.SetValue("keys", "crafting_key", _craftKey);
		Settings.Save();

		_root = new NativeMenu("Crafting", "WORKSHOP");
		MenuConrtoller.MenuPool.Add(_root);
		foreach (string category in Categories)
		{
			NativeMenu menu = new NativeMenu("Crafting", category);
			MenuConrtoller.MenuPool.Add(menu);
			_root.AddSubMenu(menu, category);
			_categoryMenus[category] = menu;
		}
		_root.Shown += delegate { Rebuild(); };
		KeyUp += OnKeyUp;
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == _craftKey && !MenuConrtoller.MenuPool.AreAnyVisible && PlayerInventory.Instance != null)
		{
			Rebuild();
			_root.Visible = true;
		}
	}

	private void Rebuild()
	{
		Inventory inventory = PlayerInventory.Instance?.CurrentInventory;
		if (inventory == null)
		{
			return;
		}
		foreach (NativeMenu menu in _categoryMenus.Values)
		{
			menu.Clear();
		}
		foreach (InventoryItemBase item in inventory.Items)
		{
			if (!(item is ICraftable craftable) || craftable.RequiredComponents == null || craftable.RequiredComponents.Length == 0)
			{
				continue;
			}
			NativeMenu target = _categoryMenus[CategoryOf(item)];
			bool canCraft = inventory.CanCraft(craftable) && item.Amount < item.MaxAmount;
			NativeItem entry = new NativeItem(item.Id, BuildRecipe(item, craftable))
			{
				AltTitle = canCraft ? "~g~Craft" : "~r~Need mats"
			};
			InventoryItemBase captured = item;
			entry.Activated += delegate
			{
				inventory.CraftItem(captured);
				Rebuild();
			};
			target.Add(entry);
		}
	}

	private static string CategoryOf(InventoryItemBase item)
	{
		if (item is FoodInventoryItem)
		{
			return "Food";
		}
		if (item is WeaponInventoryItem)
		{
			return "Weapons";
		}
		if (item is BuildableInventoryItem)
		{
			return "Building";
		}
		if (item is UsableInventoryItem)
		{
			return "Medical & Gear";
		}
		return "Tools";
	}

	private static string BuildRecipe(InventoryItemBase item, ICraftable craftable)
	{
		StringBuilder sb = new StringBuilder(item.Description ?? string.Empty);
		sb.Append("\n~s~Requires:\n");
		foreach (CraftableItemComponent c in craftable.RequiredComponents)
		{
			int have = c.Resource != null ? c.Resource.Amount : 0;
			string color = (have >= c.RequiredAmount) ? "~g~" : "~r~";
			sb.Append($"{color}{have}~s~/{c.RequiredAmount} {c.Resource?.Id}\n");
		}
		return sb.ToString();
	}
}
