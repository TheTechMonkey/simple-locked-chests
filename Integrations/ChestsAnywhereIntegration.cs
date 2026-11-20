using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LockChests.Framework;
using StardewModdingAPI;

namespace LockChests.Integrations;

/// <summary>Hides chests and storage furniture locked by other players from Chests Anywhere.</summary>
/// <remarks>
/// Chests Anywhere builds every chest list (the chest dropdown, its default chest and its lookups by tile) through
/// <c>ChestFactory.GetChests</c>, so filtering that one method hides locked chests everywhere in its UI.
/// Checked against Pathoschild/StardewMods as of November 2026.
/// </remarks>
internal static class ChestsAnywhereIntegration
{
	private const string ModId = "Pathoschild.ChestsAnywhere";
	private const string ChestFactoryTypeName = "Pathoschild.Stardew.ChestsAnywhere.ChestFactory";
	private const string ManagedChestTypeName = "Pathoschild.Stardew.ChestsAnywhere.Framework.ManagedChest";

	/// <summary>The <c>ManagedChest.MapEntity</c> property, which holds the placed chest or furniture.</summary>
	private static PropertyInfo? mapEntityProperty;

	/// <summary>Patch Chests Anywhere if it's installed.</summary>
	public static void Apply(Harmony harmony, IModRegistry modRegistry, IMonitor monitor)
	{
		if (!modRegistry.IsLoaded(ModId))
			return;

		try
		{
			Type? chestFactoryType = AccessTools.TypeByName(ChestFactoryTypeName);
			Type? managedChestType = AccessTools.TypeByName(ManagedChestTypeName);
			MethodInfo? getChests = chestFactoryType is null
				? null
				: AccessTools.GetDeclaredMethods(chestFactoryType).FirstOrDefault(method => method.Name == "GetChests");
			mapEntityProperty = managedChestType is null ? null : AccessTools.Property(managedChestType, "MapEntity");

			if (getChests is null || managedChestType is null || mapEntityProperty is null)
			{
				monitor.Log("Chests Anywhere is installed, but its chest list code has changed. Locked chests won't be hidden in it.", LogLevel.Warn);
				return;
			}

			// The postfix has to match GetChests' exact return type, IEnumerable<ManagedChest>, so make it for that type.
			MethodInfo postfix = AccessTools.Method(typeof(ChestsAnywhereIntegration), nameof(After_GetChests))
				.MakeGenericMethod(managedChestType);
			harmony.Patch(getChests, postfix: new HarmonyMethod(postfix));

			monitor.Log("Chests Anywhere integration enabled: locked chests are hidden from other players.", LogLevel.Trace);
		}
		catch (Exception ex)
		{
			monitor.Log($"Couldn't patch Chests Anywhere. Locked chests won't be hidden in it.\n{ex}", LogLevel.Warn);
		}
	}

	/// <summary>Remove chests and storage furniture locked against the local player from the results.</summary>
	private static void After_GetChests<TManagedChest>(ref IEnumerable<TManagedChest> __result)
	{
		IEnumerable<TManagedChest> chests = __result;
		__result = chests.Where(chest => !IsLockedForLocalPlayer(chest));
	}

	/// <summary>Whether a Chests Anywhere chest is locked against the local player.</summary>
	private static bool IsLockedForLocalPlayer(object? managedChest)
	{
		object? placedEntity = managedChest is null ? null : mapEntityProperty?.GetValue(managedChest);
		return LockRules.IsLockedForLocalPlayer(Lockables.FromObject(placedEntity as StardewValley.Object));
	}
}
