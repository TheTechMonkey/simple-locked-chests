using System;
using HarmonyLib;
using LockChests.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace LockChests.Patches;

/// <summary>Harmony patches that stop players from using, moving or entering things locked by someone else.</summary>
internal static class GamePatches
{
	private static IMonitor monitor = null!;

	/// <summary>Apply the patches.</summary>
	public static void Apply(Harmony harmony, IMonitor modMonitor)
	{
		monitor = modMonitor;

		// opening chests and storage furniture
		harmony.Patch(
			original: AccessTools.Method(typeof(Chest), nameof(Chest.checkForAction)),
			prefix: new HarmonyMethod(typeof(GamePatches), nameof(Before_Container_CheckForAction))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(StorageFurniture), nameof(StorageFurniture.checkForAction)),
			prefix: new HarmonyMethod(typeof(GamePatches), nameof(Before_Container_CheckForAction))
		);

		// hitting a chest with a tool, which is how chests are moved and broken in 1.6
		harmony.Patch(
			original: AccessTools.Method(typeof(Chest), nameof(Chest.performToolAction)),
			prefix: new HarmonyMethod(typeof(GamePatches), nameof(Before_Chest_PerformToolAction))
		);

		// picking up storage furniture
		harmony.Patch(
			original: AccessTools.Method(typeof(StorageFurniture), nameof(StorageFurniture.canBeRemoved)),
			prefix: new HarmonyMethod(typeof(GamePatches), nameof(Before_StorageFurniture_CanBeRemoved))
		);

		// walking through a building's front door
		harmony.Patch(
			original: AccessTools.Method(typeof(Building), nameof(Building.doAction)),
			prefix: new HarmonyMethod(typeof(GamePatches), nameof(Before_Building_DoAction))
		);
	}

	/// <summary>Block opening a locked chest or storage furniture, and claim unowned ones for the first player to open them.</summary>
	private static bool Before_Container_CheckForAction(StardewValley.Object __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
	{
		try
		{
			Lockable? lockable = Lockables.FromObject(__instance);
			if (lockable is null || who is null)
				return true;

			if (LockRules.IsLockedAgainst(lockable, who))
			{
				if (!justCheckingForActivity && who.IsLocalPlayer)
					LockMessages.ShowLockedOut(lockable);
				__result = true;
				return false;
			}

			if (!justCheckingForActivity && who.IsLocalPlayer)
				lockable.ClaimIfUnowned(who);
			return true;
		}
		catch (Exception ex)
		{
			monitor.Log($"Failed checking a container lock: {ex}", LogLevel.Error);
			return true;
		}
	}

	/// <summary>Block moving or breaking a chest locked by someone else.</summary>
	private static bool Before_Chest_PerformToolAction(Chest __instance, Tool t, ref bool __result)
	{
		try
		{
			Farmer player = t?.getLastFarmerToUse() ?? Game1.player;
			Lockable? lockable = Lockables.FromObject(__instance);
			if (!LockRules.IsLockedAgainst(lockable, player))
				return true;

			if (player.IsLocalPlayer)
				LockMessages.ShowLockedOut(lockable!);
			__result = false;
			return false;
		}
		catch (Exception ex)
		{
			monitor.Log($"Failed checking a chest lock for a tool hit: {ex}", LogLevel.Error);
			return true;
		}
	}

	/// <summary>Block picking up storage furniture locked by someone else.</summary>
	private static bool Before_StorageFurniture_CanBeRemoved(StorageFurniture __instance, Farmer who, ref bool __result)
	{
		try
		{
			Lockable? lockable = Lockables.FromObject(__instance);
			if (who is null || !LockRules.IsLockedAgainst(lockable, who))
				return true;

			if (who.IsLocalPlayer)
				LockMessages.ShowLockedOut(lockable!);
			__result = false;
			return false;
		}
		catch (Exception ex)
		{
			monitor.Log($"Failed checking a storage furniture lock for removal: {ex}", LogLevel.Error);
			return true;
		}
	}

	/// <summary>Block walking through a front door locked by someone else. Leaving is never blocked, since that's a warp from inside.</summary>
	private static bool Before_Building_DoAction(Building __instance, Vector2 tileLocation, Farmer who, ref bool __result)
	{
		try
		{
			if (who is null || !who.IsLocalPlayer || !Lockables.IsDoorTile(__instance, tileLocation))
				return true;

			Lockable? lockable = Lockables.FromDoor(__instance);
			if (!LockRules.IsLockedAgainst(lockable, who))
				return true;

			LockMessages.ShowLockedOut(lockable!);
			__result = true;
			return false;
		}
		catch (Exception ex)
		{
			monitor.Log($"Failed checking a door lock: {ex}", LogLevel.Error);
			return true;
		}
	}
}
