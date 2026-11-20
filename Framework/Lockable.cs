using StardewValley;
using StardewValley.Mods;

namespace LockChests.Framework;

/// <summary>Something that can be locked: a chest, storage furniture or a house door.</summary>
internal sealed class Lockable
{
	/// <summary>Where the owner and lock state are stored.</summary>
	public ModDataDictionary Data { get; }

	/// <summary>What kind of thing this is.</summary>
	public LockableKind Kind { get; }

	/// <summary>The owner set by the game, such as a cabin's resident, or <see cref="LockData.NoOwner"/> if the first player to lock it becomes the owner.</summary>
	public long FixedOwner { get; }

	/// <summary>The game object this wraps: a chest, storage furniture or building.</summary>
	public object Target { get; }

	/// <summary>The current owner's unique multiplayer ID, or <see cref="LockData.NoOwner"/>.</summary>
	public long Owner => this.FixedOwner != LockData.NoOwner ? this.FixedOwner : LockData.GetStoredOwner(this.Data);

	/// <summary>Whether this is open or locked.</summary>
	public LockState State => LockData.GetLockState(this.Data);

	/// <summary>Whether a player can become the owner by using it or locking it.</summary>
	public bool IsClaimable => this.FixedOwner == LockData.NoOwner && this.Owner == LockData.NoOwner;

	public Lockable(object target, ModDataDictionary data, LockableKind kind, long fixedOwner = LockData.NoOwner)
	{
		this.Target = target;
		this.Data = data;
		this.Kind = kind;
		this.FixedOwner = fixedOwner;
	}

	/// <summary>Make the given player the owner, if nobody owns it yet and it can be claimed.</summary>
	public void ClaimIfUnowned(Farmer player)
	{
		if (this.IsClaimable)
			LockData.SetStoredOwner(this.Data, player.UniqueMultiplayerID);
	}

	/// <summary>Set the lock state.</summary>
	public void SetState(LockState state)
	{
		LockData.SetLockState(this.Data, state);
	}
}
