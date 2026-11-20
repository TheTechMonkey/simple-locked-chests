using System;
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

internal static class MoveGuards
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
			Harmony harmony = new Harmony("TechMonkey.LockedChests.MoveGuards");
			bool anyPatched = false;
			anyPatched |= PatchToolMethod(harmony, monitor);
			if (anyPatched | PatchRemoveActionMethod(harmony, monitor))
			{
				monitor.Log("MoveGuards: patches applied successfully.", (LogLevel)2);
			}
			else
			{
				monitor.Log("MoveGuards: no move guard patches applied.", (LogLevel)3);
			}
		}
		catch (Exception value)
		{
			monitor.Log($"MoveGuards: failed to apply patches: {value}", (LogLevel)4);
		}
	}

	private static bool PatchToolMethod(Harmony harmony, IMonitor monitor)
	{
		MethodInfo toolMethod = TryFindExact(typeof(Chest), "performToolAction", new Type[1] { typeof(Tool) }, new Type[2]
		{
			typeof(Tool),
			typeof(GameLocation)
		});
		if (toolMethod != null)
		{
			harmony.Patch((MethodBase)toolMethod, new HarmonyMethod(typeof(MoveGuards), "Chest_performToolAction_Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			monitor.Log("MoveGuards: patched Chest.performToolAction.", (LogLevel)2);
			return true;
		}
		monitor.Log("MoveGuards: Chest.performToolAction not found; tool protection unavailable.", (LogLevel)0);
		return false;
	}

	private static bool PatchRemoveActionMethod(Harmony harmony, IMonitor monitor)
	{
		List<MethodInfo> candidates = new List<MethodInfo>();
		candidates.AddRange(FindAllByName(typeof(Chest), "performRemoveAction"));
		candidates.AddRange(FindAllByName(typeof(Object), "performRemoveAction"));
		var (chosen, chosenPrefix) = FindBestRemoveActionCandidate(candidates);
		if (chosen != null && chosenPrefix != null)
		{
			harmony.Patch((MethodBase)chosen, chosenPrefix, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			monitor.Log($"MoveGuards: patched {chosen.DeclaringType?.Name}.{chosen.Name}({string.Join(",", from p in chosen.GetParameters()
				select p.ParameterType.Name)}).", (LogLevel)2);
			return true;
		}
		monitor.Log("MoveGuards: Could not find performRemoveAction on Chest/Object - hand pickup protection unavailable.", (LogLevel)0);
		return false;
	}

	private static (MethodInfo?, HarmonyMethod?) FindBestRemoveActionCandidate(List<MethodInfo> candidates)
	{
		foreach (MethodInfo method in candidates.Distinct())
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Vector2))
			{
				return (method, new HarmonyMethod(typeof(MoveGuards), "Chest_performRemoveAction_Vec_Prefix", (Type[])null));
			}
			if (parameters.Length != 2)
			{
				continue;
			}
			if (IsVectorLocationSignature(parameters))
			{
				if (method.DeclaringType == typeof(Chest))
				{
					return (method, new HarmonyMethod(typeof(MoveGuards), "Chest_performRemoveAction_Vec_Loc_Prefix", (Type[])null));
				}
				if (method.DeclaringType == typeof(Object))
				{
					return (method, new HarmonyMethod(typeof(MoveGuards), "Object_performRemoveAction_Vec_Loc_Prefix", (Type[])null));
				}
			}
			if (IsLocationVectorSignature(parameters))
			{
				if (method.DeclaringType == typeof(Chest))
				{
					return (method, new HarmonyMethod(typeof(MoveGuards), "Chest_performRemoveAction_Loc_Vec_Prefix", (Type[])null));
				}
				if (method.DeclaringType == typeof(Object))
				{
					return (method, new HarmonyMethod(typeof(MoveGuards), "Object_performRemoveAction_Loc_Vec_Prefix", (Type[])null));
				}
			}
		}
		return (null, null);
	}

	private static bool IsVectorLocationSignature(ParameterInfo[] parameters)
	{
		if (parameters[0].ParameterType == typeof(Vector2))
		{
			return parameters[1].ParameterType == typeof(GameLocation);
		}
		return false;
	}

	private static bool IsLocationVectorSignature(ParameterInfo[] parameters)
	{
		if (parameters[0].ParameterType == typeof(GameLocation))
		{
			return parameters[1].ParameterType == typeof(Vector2);
		}
		return false;
	}

	private static MethodInfo? TryFindExact(Type type, string name, params Type[][] signatures)
	{
		foreach (Type[] signature in signatures)
		{
			try
			{
				MethodInfo method = AccessTools.Method(type, name, signature, (Type[])null);
				if (method != null)
				{
					return method;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static IEnumerable<MethodInfo> FindAllByName(Type type, string name)
	{
		try
		{
			return from m in AccessTools.GetDeclaredMethods(type)
				where m.Name == name && !m.IsStatic
				select m;
		}
		catch
		{
			return Array.Empty<MethodInfo>();
		}
	}

	private static bool Chest_performToolAction_Prefix(Chest __instance, Tool t, ref bool __result)
	{
		if (!ShouldBlockAction(__instance))
		{
			return true;
		}
		ShowLockMessage(__instance);
		__result = false;
		return false;
	}

	private static bool Chest_performRemoveAction_Vec_Prefix(Chest __instance, Vector2 tileLocation, ref bool __result)
	{
		return HandlePickup(__instance, ref __result);
	}

	private static bool Chest_performRemoveAction_Vec_Loc_Prefix(Chest __instance, Vector2 tileLocation, GameLocation location, ref bool __result)
	{
		return HandlePickup(__instance, ref __result);
	}

	private static bool Chest_performRemoveAction_Loc_Vec_Prefix(Chest __instance, GameLocation location, Vector2 tileLocation, ref bool __result)
	{
		return HandlePickup(__instance, ref __result);
	}

	private static bool Object_performRemoveAction_Vec_Loc_Prefix(Object __instance, Vector2 tileLocation, GameLocation location, ref bool __result)
	{
		Chest chest = (Chest)(object)((__instance is Chest) ? __instance : null);
		if (chest == null || !((NetFieldBase<bool, NetBool>)(object)chest.playerChest).Value)
		{
			return true;
		}
		return HandlePickup(chest, ref __result);
	}

	private static bool Object_performRemoveAction_Loc_Vec_Prefix(Object __instance, GameLocation location, Vector2 tileLocation, ref bool __result)
	{
		Chest chest = (Chest)(object)((__instance is Chest) ? __instance : null);
		if (chest == null || !((NetFieldBase<bool, NetBool>)(object)chest.playerChest).Value)
		{
			return true;
		}
		return HandlePickup(chest, ref __result);
	}

	private static bool HandlePickup(Chest chest, ref bool __result)
	{
		if (!ShouldBlockAction(chest))
		{
			return true;
		}
		ShowLockMessage(chest);
		__result = true;
		return false;
	}

	private static bool ShouldBlockAction(Chest chest)
	{
		try
		{
			if (!((NetFieldBase<bool, NetBool>)(object)chest.playerChest).Value)
			{
				return false;
			}
			if (ModEntry.ParseLock(chest) != Lock.Locked)
			{
				return false;
			}
			long owner = ModEntry.ParseOwner(chest);
			return owner != -1 && owner != Game1.player.UniqueMultiplayerID;
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"MoveGuards ShouldBlockAction error: {value}", (LogLevel)3);
			}
			return true;
		}
	}

	private static void ShowLockMessage(Chest chest)
	{
		try
		{
			Game1.playSound("cancel", (int?)null);
			long owner = ModEntry.ParseOwner(chest);
			Farmer ownerFarmer = ModEntry.TryGetPlayer(owner);
			string ownerName = ((ownerFarmer != null) ? ((Character)ownerFarmer).Name : null) ?? "Another player";
			string message = Translation.op_Implicit(ModEntry.ITranslations.Get("No_Access_Dialogue", (object)new
			{
				Name = ownerName
			}));
			Game1.showRedMessage(message, true);
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"MoveGuards ShowLockMessage error: {value}", (LogLevel)3);
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
