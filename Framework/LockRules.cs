using System;
using StardewValley;

namespace LockChests.Framework;

/// <summary>Decides who can use, lock and unlock things.</summary>
internal static class LockRules
{
	/// <summary>Gets whether the host can open and unlock anything. Set from config in <see cref="Init"/>.</summary>
	private static Func<bool> hostOverrideEnabled = () => false;

	/// <summary>Set where the host override setting is read from.</summary>
	public static void Init(Func<bool> getHostOverride)
	{
		hostOverrideEnabled = getHostOverride;
	}

	/// <summary>Whether the player bypasses locks because they're the host and the host override is on.</summary>
	private static bool HasHostOverride(Farmer player)
	{
		return player.IsMainPlayer && hostOverrideEnabled();
	}

	/// <summary>Whether the player is kept out: it's locked, someone else owns it, and the host override doesn't apply.</summary>
	public static bool IsLockedAgainst(Lockable? lockable, Farmer player)
	{
		if (lockable is null || lockable.State != LockState.Locked)
			return false;

		long owner = lockable.Owner;
		return owner != LockData.NoOwner
			&& owner != player.UniqueMultiplayerID
			&& !HasHostOverride(player);
	}

	/// <summary>Whether the local player is kept out.</summary>
	public static bool IsLockedForLocalPlayer(Lockable? lockable)
	{
		return IsLockedAgainst(lockable, Game1.player);
	}

	/// <summary>Whether the player can lock and unlock it: they own it, nobody owns it yet, or the host override applies.</summary>
	public static bool CanManage(Lockable lockable, Farmer player)
	{
		return lockable.IsOwnedBy(player)
			|| lockable.IsClaimable
			|| HasHostOverride(player);
	}

	/// <summary>Get the display name of an owner.</summary>
	public static string GetOwnerName(long ownerId)
	{
		Farmer? owner = ownerId == LockData.NoOwner ? null : Game1.GetPlayer(ownerId);
		return !string.IsNullOrWhiteSpace(owner?.Name)
			? owner.Name
			: I18n.Get("Unknown_Player", "Unknown Player");
	}
}
