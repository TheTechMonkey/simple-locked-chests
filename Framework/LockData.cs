using System;
using StardewValley.Mods;

namespace LockChests.Framework;

/// <summary>Reads and writes lock data in an object's or building's <c>modData</c>, which the game saves and syncs in multiplayer.</summary>
/// <remarks>The keys are unchanged from earlier versions, so locks in existing saves keep working.</remarks>
internal static class LockData
{
	/// <summary>The <c>modData</c> key for the owner's unique multiplayer ID.</summary>
	public const string OwnerKey = "LockedChests.Owner";

	/// <summary>The <c>modData</c> key for the <see cref="LockState"/>.</summary>
	public const string LockKey = "LockedChests.Lock";

	/// <summary>The owner ID used when nobody owns something.</summary>
	public const long NoOwner = -1;

	/// <summary>Get the owner ID stored in the data, or <see cref="NoOwner"/>.</summary>
	public static long GetStoredOwner(ModDataDictionary data)
	{
		return data.TryGetValue(OwnerKey, out string? rawOwner) && long.TryParse(rawOwner, out long ownerId)
			? ownerId
			: NoOwner;
	}

	/// <summary>Store an owner ID.</summary>
	public static void SetStoredOwner(ModDataDictionary data, long ownerId)
	{
		data[OwnerKey] = ownerId.ToString();
	}

	/// <summary>Get the stored lock state, or <see cref="LockState.Open"/> if none is stored.</summary>
	public static LockState GetLockState(ModDataDictionary data)
	{
		return data.TryGetValue(LockKey, out string? rawState) && Enum.TryParse(rawState, out LockState state)
			? state
			: LockState.Open;
	}

	/// <summary>Store a lock state.</summary>
	public static void SetLockState(ModDataDictionary data, LockState state)
	{
		data[LockKey] = state.ToString();
	}
}
