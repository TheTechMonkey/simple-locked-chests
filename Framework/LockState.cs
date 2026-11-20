namespace LockChests.Framework;

/// <summary>Whether a chest is open to everyone or restricted to its owner.</summary>
public enum LockState
{
	/// <summary>Anyone can open, move or break the chest.</summary>
	Open,

	/// <summary>Only the owner can open, move or break the chest.</summary>
	Locked
}
