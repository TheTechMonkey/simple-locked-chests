using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace LockChests;

internal static class ChestsAnywhereCompat
{
	private static bool _patched;

	private static readonly object _patchLock = new object();

	public static void Patch(IMonitor monitor)
	{
		lock (_patchLock)
		{
			if (_patched)
			{
				return;
			}
			try
			{
				Harmony harmony = new Harmony("TechMonkey.LockedChests.ThirdPartyCompat");
				IModInfo chestsAnywhere = ModEntry.IHelper.ModRegistry.Get("Pathoschild.ChestsAnywhere");
				if (chestsAnywhere != null)
				{
					PatchChestsAnywhere(harmony, monitor);
				}
				IModInfo allChestsMenu = ModEntry.IHelper.ModRegistry.Get("aedenthorn.AllChestsMenu");
				if (allChestsMenu != null)
				{
					PatchAllChestsMenu(harmony, monitor);
				}
				_patched = true;
				if (chestsAnywhere != null || allChestsMenu != null)
				{
					monitor.Log("Third-party chest mod integration active.", (LogLevel)2);
				}
			}
			catch (Exception value)
			{
				monitor.Log($"Failed to patch third-party chest mods: {value}", (LogLevel)4);
			}
		}
	}

	private static void PatchChestsAnywhere(Harmony harmony, IMonitor monitor)
	{
		try
		{
			Assembly caAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => string.Equals(a.GetName().Name, "ChestsAnywhere", StringComparison.OrdinalIgnoreCase));
			if (caAsm == null)
			{
				return;
			}
			Type chestFactory = caAsm.GetType("Pathoschild.Stardew.ChestsAnywhere.ChestFactory");
			if (chestFactory == null)
			{
				return;
			}
			MethodInfo[] candidates = (from m in chestFactory.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				where m.Name == "GetChests"
				select m).ToArray();
			MethodInfo target = candidates.FirstOrDefault(delegate(MethodInfo m)
			{
				ParameterInfo[] parameters = m.GetParameters();
				return parameters.Length >= 2 && parameters[0].ParameterType.Name.Contains("RangeHandler") && parameters[1].ParameterType == typeof(bool);
			});
			if (target != null)
			{
				MethodInfo postfix = typeof(ChestsAnywhereCompat).GetMethod("ChestsAnywhere_GetChests_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
				if (postfix != null)
				{
					harmony.Patch((MethodBase)target, (HarmonyMethod)null, new HarmonyMethod(postfix), (HarmonyMethod)null, (HarmonyMethod)null);
					monitor.Log("Patched ChestsAnywhere", (LogLevel)0);
				}
			}
		}
		catch (Exception value)
		{
			monitor.Log($"ChestsAnywhere patch failed: {value}", (LogLevel)3);
		}
	}

	private static void PatchAllChestsMenu(Harmony harmony, IMonitor monitor)
	{
		try
		{
			Assembly acmAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.GetName().Name?.Contains("AllChestsMenu", StringComparison.OrdinalIgnoreCase) ?? false);
			if (acmAsm == null)
			{
				return;
			}
			Type allChestsMenuType = acmAsm.GetType("AllChestsMenu.AllChestsMenu");
			if (allChestsMenuType == null)
			{
				return;
			}
			MethodInfo resetMethod = allChestsMenuType.GetMethod("ResetAllChestList", BindingFlags.Instance | BindingFlags.NonPublic);
			if (resetMethod != null)
			{
				MethodInfo postfix = typeof(ChestsAnywhereCompat).GetMethod("AllChestsMenu_ResetAllChestList_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
				if (postfix != null)
				{
					harmony.Patch((MethodBase)resetMethod, (HarmonyMethod)null, new HarmonyMethod(postfix), (HarmonyMethod)null, (HarmonyMethod)null);
					monitor.Log("Patched All Chests Menu ResetAllChestList", (LogLevel)0);
				}
			}
			ConstructorInfo[] constructors = allChestsMenuType.GetConstructors();
			ConstructorInfo mainConstructor = constructors.FirstOrDefault((ConstructorInfo c) => c.GetParameters().Length == 0);
			if (mainConstructor != null)
			{
				MethodInfo postfixCtor = typeof(ChestsAnywhereCompat).GetMethod("AllChestsMenu_Constructor_Postfix", BindingFlags.Static | BindingFlags.NonPublic);
				if (postfixCtor != null)
				{
					harmony.Patch((MethodBase)mainConstructor, (HarmonyMethod)null, new HarmonyMethod(postfixCtor), (HarmonyMethod)null, (HarmonyMethod)null);
					monitor.Log("Patched All Chests Menu constructor", (LogLevel)0);
				}
			}
		}
		catch (Exception value)
		{
			monitor.Log($"All Chests Menu patch failed: {value}", (LogLevel)3);
		}
	}

	private static void ChestsAnywhere_GetChests_Postfix(ref object __result)
	{
		if (__result == null || !(__result is IEnumerable enumerable) || !ModEntry.IConfig.FilterThirdPartyMods)
		{
			return;
		}
		try
		{
			List<object> filteredChests = new List<object>();
			Type elemType = null;
			foreach (object managedChest in enumerable)
			{
				if (managedChest == null)
				{
					continue;
				}
				if (elemType == null)
				{
					elemType = managedChest.GetType();
				}
				if (TryGetChestFromManagedChest(managedChest, out Chest chest))
				{
					if (!ShouldHideChestFromPlayer(chest))
					{
						filteredChests.Add(managedChest);
					}
				}
				else
				{
					filteredChests.Add(managedChest);
				}
			}
			if (elemType != null)
			{
				Array result = Array.CreateInstance(elemType, filteredChests.Count);
				for (int i = 0; i < filteredChests.Count; i++)
				{
					result.SetValue(filteredChests[i], i);
				}
				__result = result;
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error filtering ChestsAnywhere results: {value}", (LogLevel)4);
			}
		}
	}

	private static void AllChestsMenu_ResetAllChestList_Postfix(object __instance)
	{
		if (!ModEntry.IConfig.FilterThirdPartyMods)
		{
			return;
		}
		try
		{
			FieldInfo allChestDataListField = __instance.GetType().GetField("allChestDataList", BindingFlags.Instance | BindingFlags.NonPublic);
			if (allChestDataListField == null)
			{
				return;
			}
			object allChestDataListObj = allChestDataListField.GetValue(__instance);
			if (allChestDataListObj == null)
			{
				return;
			}
			Type listType = allChestDataListObj.GetType();
			if (!typeof(IList).IsAssignableFrom(listType))
			{
				return;
			}
			IList allChestDataList = (IList)allChestDataListObj;
			for (int i = allChestDataList.Count - 1; i >= 0; i--)
			{
				object chestDataObj = allChestDataList[i];
				if (chestDataObj != null)
				{
					object? obj = chestDataObj.GetType().GetProperty("chest")?.GetValue(chestDataObj);
					Chest chest = (Chest)((obj is Chest) ? obj : null);
					if (chest != null && ShouldHideChestFromPlayer(chest))
					{
						allChestDataList.RemoveAt(i);
					}
				}
			}
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log("Filtered All Chests Menu, removed locked chests owned by others", (LogLevel)0);
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor2 = ModEntry.IMonitor;
			if (iMonitor2 != null)
			{
				iMonitor2.Log($"Error filtering All Chests Menu: {value}", (LogLevel)4);
			}
		}
	}

	private static void AllChestsMenu_Constructor_Postfix(object __instance)
	{
		AllChestsMenu_ResetAllChestList_Postfix(__instance);
	}

	private static bool TryGetChestFromManagedChest(object managedChest, out Chest? chest)
	{
		chest = null;
		try
		{
			Type type = managedChest.GetType();
			PropertyInfo locProp = type.GetProperty("Location", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo tileProp = type.GetProperty("Tile", BindingFlags.Instance | BindingFlags.Public);
			object? obj = locProp?.GetValue(managedChest);
			GameLocation location = (GameLocation)((obj is GameLocation) ? obj : null);
			Object obj2 = default(Object);
			if (location != null && tileProp?.GetValue(managedChest) is Vector2 tile && location.Objects.TryGetValue(tile, ref obj2))
			{
				Chest foundChest = (Chest)(object)((obj2 is Chest) ? obj2 : null);
				if (foundChest != null && ((NetFieldBase<bool, NetBool>)(object)foundChest.playerChest).Value)
				{
					chest = foundChest;
					return true;
				}
			}
			object? obj3 = type.GetProperty("Chest", BindingFlags.Instance | BindingFlags.Public)?.GetValue(managedChest);
			Chest directChest = (Chest)((obj3 is Chest) ? obj3 : null);
			if (directChest != null && ((NetFieldBase<bool, NetBool>)(object)directChest.playerChest).Value)
			{
				chest = directChest;
				return true;
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error resolving ManagedChest: {value}", (LogLevel)0);
			}
		}
		return false;
	}

	private static bool ShouldHideChestFromPlayer(Chest? chest)
	{
		try
		{
			if (chest == null || !((NetFieldBase<bool, NetBool>)(object)chest.playerChest).Value)
			{
				return false;
			}
			Lock lockState = ModEntry.ParseLock(chest);
			if (lockState != Lock.Locked)
			{
				return false;
			}
			long owner = ModEntry.ParseOwner(chest);
			if (owner == -1 || owner == Game1.player.UniqueMultiplayerID)
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}
