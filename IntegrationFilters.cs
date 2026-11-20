using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LockChests;

internal static class IntegrationFilters
{
	private static readonly BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly IList<Item> _blankItems = new List<Item>(0);

	private static readonly List<Item> _blankList = new List<Item>(0);

	public static void FilterActiveThirdPartyMenu(object? menu)
	{
		FilterIfThirdPartyChestBrowser(menu);
	}

	public static void FilterIfThirdPartyChestBrowser(object? menu)
	{
		if (menu == null || !Is3rdChestBrowser(menu))
		{
			return;
		}
		try
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log("Filtering third-party chest browser menu", (LogLevel)0);
			}
			PruneMenuContainers(menu);
		}
		catch (Exception value)
		{
			IMonitor? iMonitor2 = ModEntry.IMonitor;
			if (iMonitor2 != null)
			{
				iMonitor2.Log($"Error filtering third-party menu: {value}", (LogLevel)4);
			}
		}
	}

	public static void HideLockedChestContentsIfNonOwner()
	{
		IClickableMenu menu = Game1.activeClickableMenu;
		if (menu == null)
		{
			return;
		}
		try
		{
			ItemGrabMenu igm = (ItemGrabMenu)(object)((menu is ItemGrabMenu) ? menu : null);
			if (igm != null && TryGetBoundChestForIGM(igm, out Chest chest) && chest != null && MustHideForLocal(chest))
			{
				BlankIGM(igm);
			}
			foreach (ItemGrabMenu igmInner in EnumerateEmbeddedIGMs(menu))
			{
				if (TryGetBoundChestForIGM(igmInner, out Chest chest2) && chest2 != null && MustHideForLocal(chest2))
				{
					BlankIGM(igmInner);
				}
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error hiding locked chest contents: {value}", (LogLevel)4);
			}
		}
	}

	public static void FilterActiveThirdPartyMenu()
	{
		FilterIfThirdPartyChestBrowser(Game1.activeClickableMenu);
	}

	public static bool IsThirdPartyChestBrowser(object menu)
	{
		return Is3rdChestBrowser(menu);
	}

	private static bool Is3rdChestBrowser(object menu)
	{
		if (menu == null)
		{
			return false;
		}
		string typeName = menu.GetType().Name;
		if (typeName == "OwnerSettingsMenu")
		{
			return false;
		}
		Type t = menu.GetType();
		string full = t.FullName ?? string.Empty;
		string asm = t.Assembly?.GetName().Name ?? string.Empty;
		if (full.Contains("ChestsAnywhere", StringComparison.OrdinalIgnoreCase) || asm.Contains("ChestsAnywhere", StringComparison.OrdinalIgnoreCase) || asm.Contains("Pathoschild.Stardew.ChestsAnywhere", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (full.Contains("AllChests", StringComparison.OrdinalIgnoreCase) || full.Contains("All Chests", StringComparison.OrdinalIgnoreCase) || asm.Contains("AllChests", StringComparison.OrdinalIgnoreCase) || typeName.Contains("AllChests", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	private static void PruneMenuContainers(object menu)
	{
		Type type = menu.GetType();
		MemberInfo[] members = type.GetMembers(Inst);
		foreach (MemberInfo m in members)
		{
			Type memberType = null;
			if (m is FieldInfo f)
			{
				memberType = f.FieldType;
			}
			else if (m is PropertyInfo { CanRead: not false } p)
			{
				memberType = p.PropertyType;
			}
			if (memberType != null)
			{
				object val = GetMemberValue(menu, m);
				if (val is IList list)
				{
					PruneList(list);
				}
				else if (val is IDictionary dict)
				{
					PruneDict(dict);
				}
			}
		}
	}

	private static void PruneList(IList list)
	{
		for (int i = list.Count - 1; i >= 0; i--)
		{
			object entry = list[i];
			if (entry != null)
			{
				if (ResolveChest(entry, out Chest chest) && chest != null)
				{
					if (MustHideForLocal(chest))
					{
						IMonitor? iMonitor = ModEntry.IMonitor;
						if (iMonitor != null)
						{
							iMonitor.Log($"Removing locked chest from list at index {i}", (LogLevel)0);
						}
						list.RemoveAt(i);
					}
				}
				else if (entry is IList nested)
				{
					PruneList(nested);
				}
				else if (entry is IDictionary nestedDict)
				{
					PruneDict(nestedDict);
				}
			}
		}
	}

	private static void PruneDict(IDictionary dict)
	{
		List<object> toRemove = new List<object>();
		foreach (DictionaryEntry kv in dict)
		{
			object val = kv.Value;
			if (val == null)
			{
				continue;
			}
			if (ResolveChest(val, out Chest chest) && chest != null)
			{
				if (MustHideForLocal(chest))
				{
					IMonitor? iMonitor = ModEntry.IMonitor;
					if (iMonitor != null)
					{
						iMonitor.Log("Removing locked chest from dictionary", (LogLevel)0);
					}
					toRemove.Add(kv.Key);
				}
			}
			else if (val is IList lst)
			{
				PruneList(lst);
			}
			else if (val is IDictionary nested)
			{
				PruneDict(nested);
			}
		}
		foreach (object key in toRemove)
		{
			dict.Remove(key);
		}
	}

	private static bool ResolveChest(object entry, out Chest? chest)
	{
		chest = null;
		if (entry == null)
		{
			return false;
		}
		Chest c = (Chest)((entry is Chest) ? entry : null);
		if (c != null && ((NetFieldBase<bool, NetBool>)(object)c.playerChest).Value)
		{
			chest = c;
			return true;
		}
		Type t = entry.GetType();
		object location = null;
		object tile = null;
		PropertyInfo[] properties = t.GetProperties(Inst);
		foreach (PropertyInfo prop in properties)
		{
			if (prop.PropertyType == typeof(GameLocation) && prop.Name.Contains("Location", StringComparison.OrdinalIgnoreCase))
			{
				location = prop.GetValue(entry);
			}
			else if (prop.PropertyType == typeof(Vector2) && prop.Name.Contains("Tile", StringComparison.OrdinalIgnoreCase))
			{
				tile = prop.GetValue(entry);
			}
		}
		FieldInfo[] fields = t.GetFields(Inst);
		foreach (FieldInfo field in fields)
		{
			if (field.FieldType == typeof(GameLocation) && field.Name.Contains("Location", StringComparison.OrdinalIgnoreCase))
			{
				location = field.GetValue(entry);
			}
			else if (field.FieldType == typeof(Vector2) && field.Name.Contains("Tile", StringComparison.OrdinalIgnoreCase))
			{
				tile = field.GetValue(entry);
			}
		}
		GameLocation loc = (GameLocation)((location is GameLocation) ? location : null);
		Object obj = default(Object);
		if (loc != null && tile is Vector2 tilePos && loc.Objects.TryGetValue(tilePos, ref obj))
		{
			Chest foundChest = (Chest)(object)((obj is Chest) ? obj : null);
			if (foundChest != null && ((NetFieldBase<bool, NetBool>)(object)foundChest.playerChest).Value)
			{
				chest = foundChest;
				return true;
			}
		}
		MemberInfo[] members = t.GetMembers(Inst);
		foreach (MemberInfo member in members)
		{
			object val = GetMemberValue(entry, member);
			if (val is IList<Item> itemList)
			{
				Chest indexedChest = ChestIndex.TryFindByItemsList(itemList);
				if (indexedChest != null && ((NetFieldBase<bool, NetBool>)(object)indexedChest.playerChest).Value)
				{
					chest = indexedChest;
					return true;
				}
			}
		}
		string[] wrapperNames = new string[7] { "Container", "Chest", "Underlying", "Object", "Target", "Context", "Storage" };
		string[] array = wrapperNames;
		foreach (string name in array)
		{
			object val2 = GetMemberValueByName(entry, t, name);
			Chest wrapperChest = (Chest)((val2 is Chest) ? val2 : null);
			if (wrapperChest != null && ((NetFieldBase<bool, NetBool>)(object)wrapperChest.playerChest).Value)
			{
				chest = wrapperChest;
				return true;
			}
		}
		return false;
	}

	private static IEnumerable<ItemGrabMenu> EnumerateEmbeddedIGMs(object root)
	{
		Type t = root.GetType();
		MemberInfo[] members = t.GetMembers(Inst);
		foreach (MemberInfo m in members)
		{
			Type memberType = null;
			if (m is FieldInfo f)
			{
				memberType = f.FieldType;
			}
			else if (m is PropertyInfo { CanRead: not false } p)
			{
				memberType = p.PropertyType;
			}
			if (memberType != null && typeof(ItemGrabMenu).IsAssignableFrom(memberType))
			{
				object? memberValue = GetMemberValue(root, m);
				ItemGrabMenu igm = (ItemGrabMenu)((memberValue is ItemGrabMenu) ? memberValue : null);
				if (igm != null)
				{
					yield return igm;
				}
			}
		}
	}

	private static bool TryGetBoundChestForIGM(ItemGrabMenu igm, out Chest? chest)
	{
		chest = null;
		object context = igm.context;
		Chest c = (Chest)((context is Chest) ? context : null);
		if (c != null && ((NetFieldBase<bool, NetBool>)(object)c.playerChest).Value)
		{
			chest = c;
			return true;
		}
		IList<Item> list = igm.ItemsToGrabMenu?.actualInventory;
		if (list != null)
		{
			Chest c2 = ChestIndex.TryFindByItemsList(list);
			if (c2 != null && ((NetFieldBase<bool, NetBool>)(object)c2.playerChest).Value)
			{
				chest = c2;
				return true;
			}
		}
		return false;
	}

	private static void BlankIGM(ItemGrabMenu igm)
	{
		try
		{
			InventoryMenu grid = igm.ItemsToGrabMenu;
			if (grid == null)
			{
				return;
			}
			Type gridType = ((object)grid).GetType();
			FieldInfo fiActual = gridType.GetField("actualInventory", Inst);
			if (fiActual != null)
			{
				fiActual.SetValue(grid, _blankItems);
			}
			else
			{
				PropertyInfo piActual = gridType.GetProperty("actualInventory", Inst) ?? gridType.GetProperty("ActualInventory", Inst);
				if ((object)piActual != null && piActual.CanWrite)
				{
					piActual.SetValue(grid, _blankItems);
				}
			}
			FieldInfo fiInv = gridType.GetField("inventory", Inst);
			if (fiInv != null)
			{
				fiInv.SetValue(grid, _blankList);
			}
			else
			{
				PropertyInfo piInv = gridType.GetProperty("inventory", Inst) ?? gridType.GetProperty("Inventory", Inst);
				if ((object)piInv != null && piInv.CanWrite)
				{
					piInv.SetValue(grid, _blankList);
				}
			}
			typeof(ItemGrabMenu).GetField("hoveredItem", Inst)?.SetValue(igm, null);
			typeof(ItemGrabMenu).GetField("heldItem", Inst)?.SetValue(igm, null);
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error blanking ItemGrabMenu: {value}", (LogLevel)4);
			}
		}
	}

	private static bool MustHideForLocal(Chest c)
	{
		if (!((NetFieldBase<bool, NetBool>)(object)c.playerChest).Value)
		{
			return false;
		}
		long owner = ModEntry.ParseOwner(c);
		if (owner == -1 || owner == Game1.player.UniqueMultiplayerID)
		{
			return false;
		}
		return ModEntry.ParseLock(c) == Lock.Locked;
	}

	private static object? GetMemberValue(object obj, MemberInfo m)
	{
		try
		{
			if (m is FieldInfo f)
			{
				return f.GetValue(obj);
			}
			if (m is PropertyInfo { CanRead: not false } p)
			{
				return p.GetValue(obj);
			}
		}
		catch
		{
		}
		return null;
	}

	private static object? GetMemberValueByName(object obj, Type t, string name)
	{
		try
		{
			PropertyInfo p = t.GetProperty(name, Inst);
			if ((object)p != null && p.CanRead)
			{
				return p.GetValue(obj);
			}
			FieldInfo f = t.GetField(name, Inst);
			if (f != null)
			{
				return f.GetValue(obj);
			}
		}
		catch
		{
		}
		return null;
	}
}
