using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace LockChests;

internal static class AllChestsMenuCompat
{
	private static Type? _acmType;

	private static FieldInfo? _fiAllChestList;

	private static FieldInfo? _fiChestList;

	private static FieldInfo? _fiChestOnChestData;

	public static void Patch(IMonitor monitor)
	{
		try
		{
			_acmType = AccessTools.TypeByName("AllChestsMenu.AllChestsMenu");
			if (_acmType == null)
			{
				monitor.Log("AllChestsMenu not found; skipping compat.", (LogLevel)0);
				return;
			}
			_fiAllChestList = AccessTools.Field(_acmType, "allChestDataList");
			_fiChestList = AccessTools.Field(_acmType, "chestDataList");
			Type chestDataType = AccessTools.TypeByName("AllChestsMenu.ChestData");
			if (chestDataType != null)
			{
				_fiChestOnChestData = AccessTools.Field(chestDataType, "chest");
			}
			Harmony harmony = new Harmony(ModEntry.Manifest.UniqueID + ".AllChestsMenuCompat");
			MethodInfo mResetAll = AccessTools.Method(_acmType, "ResetAllChestList", (Type[])null, (Type[])null);
			if (mResetAll != null)
			{
				harmony.Patch((MethodBase)mResetAll, (HarmonyMethod)null, new HarmonyMethod(typeof(AllChestsMenuCompat), "Post_ResetAllChestList", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
			}
			MethodInfo mReset = AccessTools.Method(_acmType, "ResetChestList", (Type[])null, (Type[])null);
			if (mReset != null)
			{
				harmony.Patch((MethodBase)mReset, (HarmonyMethod)null, new HarmonyMethod(typeof(AllChestsMenuCompat), "Post_ResetChestList", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
			}
			monitor.Log("AllChestsMenu compat: filtering locked chests enabled.", (LogLevel)2);
		}
		catch (Exception value)
		{
			monitor.Log($"AllChestsMenu compat failed: {value}", (LogLevel)3);
		}
	}

	private static void Post_ResetAllChestList(object __instance)
	{
		try
		{
			if (!(_fiAllChestList == null) && !(_fiChestOnChestData == null) && _fiAllChestList.GetValue(__instance) is IList list)
			{
				RemoveLockedEntries(list);
			}
		}
		catch
		{
		}
	}

	private static void Post_ResetChestList(object __instance)
	{
		try
		{
			if (!(_fiChestList == null) && !(_fiChestOnChestData == null) && _fiChestList.GetValue(__instance) is IList list)
			{
				RemoveLockedEntries(list);
			}
		}
		catch
		{
		}
	}

	private static void RemoveLockedEntries(IList chestDataList)
	{
		for (int i = chestDataList.Count - 1; i >= 0; i--)
		{
			object chestData = chestDataList[i];
			if (chestData != null)
			{
				object? obj = _fiChestOnChestData?.GetValue(chestData);
				Chest chest = (Chest)((obj is Chest) ? obj : null);
				if (ShouldHideForLocal(chest))
				{
					chestDataList.RemoveAt(i);
				}
			}
		}
	}

	private static bool ShouldHideForLocal(Chest? c)
	{
		if (c == null || !((NetFieldBase<bool, NetBool>)(object)c.playerChest).Value)
		{
			return false;
		}
		Lock lockState = ModEntry.ParseLock(c);
		if (lockState != Lock.Locked)
		{
			return false;
		}
		long owner = ModEntry.ParseOwner(c);
		if (owner == -1)
		{
			return false;
		}
		return owner != Game1.player.UniqueMultiplayerID;
	}
}
