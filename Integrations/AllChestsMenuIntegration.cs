using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using LockChests.Framework;
using StardewModdingAPI;
using StardewValley.Objects;

namespace LockChests.Integrations;

/// <summary>Hides chests locked by other players from aedenthorn's All Chests Menu.</summary>
/// <remarks>
/// All Chests Menu rebuilds its full chest list in <c>ResetAllChestList()</c> and every view, search and transfer reads from
/// that list, so removing locked chests from it after each rebuild hides them everywhere in the menu.
/// Checked against All Chests Menu 0.4.2 as of November 2026.
/// </remarks>
internal static class AllChestsMenuIntegration
{
	private const string ModId = "aedenthorn.AllChestsMenu";
	private const string MenuTypeName = "AllChestsMenu.AllChestsMenu";
	private const string ChestDataTypeName = "AllChestsMenu.ChestData";

	/// <summary>The menu's <c>allChestDataList</c> field.</summary>
	private static FieldInfo? allChestDataListField;

	/// <summary>The <c>ChestData.chest</c> field.</summary>
	private static FieldInfo? chestField;

	/// <summary>Patch All Chests Menu if it's installed.</summary>
	public static void Apply(Harmony harmony, IModRegistry modRegistry, IMonitor monitor)
	{
		if (!modRegistry.IsLoaded(ModId))
			return;

		try
		{
			Type? menuType = AccessTools.TypeByName(MenuTypeName);
			Type? chestDataType = AccessTools.TypeByName(ChestDataTypeName);
			MethodInfo? resetAllChestList = menuType is null ? null : AccessTools.Method(menuType, "ResetAllChestList");
			allChestDataListField = menuType is null ? null : AccessTools.Field(menuType, "allChestDataList");
			chestField = chestDataType is null ? null : AccessTools.Field(chestDataType, "chest");

			if (resetAllChestList is null || allChestDataListField is null || chestField is null)
			{
				monitor.Log("All Chests Menu is installed, but its chest list code has changed. Locked chests won't be hidden in it.", LogLevel.Warn);
				return;
			}

			harmony.Patch(resetAllChestList, postfix: new HarmonyMethod(typeof(AllChestsMenuIntegration), nameof(After_ResetAllChestList)));
			monitor.Log("All Chests Menu integration enabled: locked chests are hidden from other players.", LogLevel.Trace);
		}
		catch (Exception ex)
		{
			monitor.Log($"Couldn't patch All Chests Menu. Locked chests won't be hidden in it.\n{ex}", LogLevel.Warn);
		}
	}

	/// <summary>Remove chests locked against the local player from the menu's chest list.</summary>
	private static void After_ResetAllChestList(object __instance)
	{
		if (allChestDataListField?.GetValue(__instance) is not IList chestDataList)
			return;

		for (int index = chestDataList.Count - 1; index >= 0; index--)
		{
			object? chestData = chestDataList[index];
			Chest? chest = chestData is null ? null : chestField?.GetValue(chestData) as Chest;
			if (LockRules.IsLockedForLocalPlayer(Lockables.FromObject(chest)))
				chestDataList.RemoveAt(index);
		}
	}
}
