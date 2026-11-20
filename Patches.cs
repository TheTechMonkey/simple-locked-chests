using System;
using System.Reflection;
using HarmonyLib;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace LockChests;

internal static class Patches
{
	private static bool _patched;

	public static void Patch(IMonitor monitor)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		try
		{
			Harmony harmony = new Harmony("TechMonkey.LockedChests.Core");
			MethodInfo checkForActionMethod = AccessTools.Method(typeof(Chest), "checkForAction", new Type[2]
			{
				typeof(Farmer),
				typeof(bool)
			}, (Type[])null);
			if (checkForActionMethod != null)
			{
				HarmonyMethod prefix = new HarmonyMethod(typeof(Patches), "Chest_checkForAction_Prefix", (Type[])null);
				harmony.Patch((MethodBase)checkForActionMethod, prefix, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				monitor.Log("Core patches applied successfully.", (LogLevel)2);
			}
			else
			{
				monitor.Log("Warning: Could not find Chest.checkForAction method.", (LogLevel)3);
			}
		}
		catch (Exception value)
		{
			monitor.Log($"Failed to apply core patches: {value}", (LogLevel)4);
		}
	}

	private static bool Chest_checkForAction_Prefix(Chest __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
	{
		try
		{
			if (!((NetFieldBase<bool, NetBool>)(object)__instance.playerChest).Value)
			{
				return true;
			}
			Lock lockState = ModEntry.ParseLock(__instance);
			if (lockState != Lock.Locked)
			{
				return true;
			}
			long owner = ModEntry.ParseOwner(__instance);
			if (owner == -1 || owner == who.UniqueMultiplayerID)
			{
				return true;
			}
			if (!justCheckingForActivity)
			{
				ShowAccessDeniedMessage(owner);
			}
			__result = true;
			return false;
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error in Chest_checkForAction_Prefix: {value}", (LogLevel)4);
			}
			return true;
		}
	}

	private static void ShowAccessDeniedMessage(long ownerId)
	{
		try
		{
			Game1.playSound("cancel", (int?)null);
			Farmer ownerFarmer = ModEntry.TryGetPlayer(ownerId);
			string ownerName = ((ownerFarmer != null) ? ((Character)ownerFarmer).Name : null) ?? "Another player";
			string message = ((object)ModEntry.ITranslations.Get("No_Access_Dialogue", (object)new
			{
				Name = ownerName
			})).ToString();
			if (string.IsNullOrEmpty(message) || message.Contains("No_Access_Dialogue"))
			{
				message = ownerName + " is the owner and has restricted this chest.";
			}
			Game1.showRedMessage(message, true);
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error showing access denied message: {value}", (LogLevel)3);
			}
			try
			{
				Game1.showRedMessage("This chest is locked.", true);
			}
			catch
			{
				Game1.playSound("cancel", (int?)null);
			}
		}
	}
}
