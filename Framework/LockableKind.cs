namespace LockChests.Framework;

/// <summary>The kinds of things that can be locked.</summary>
internal enum LockableKind
{
	/// <summary>A player-placed chest.</summary>
	Chest,

	/// <summary>Furniture that stores items, like a dresser.</summary>
	StorageFurniture,

	/// <summary>The front door of a farmhouse, cabin or shed.</summary>
	Door
}
