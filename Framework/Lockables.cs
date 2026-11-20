using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace LockChests.Framework;

/// <summary>Finds lockable things and decides which game objects and buildings can be locked.</summary>
internal static class Lockables
{
	/// <summary>Building types whose front door can be locked by the first player to lock it.</summary>
	private static readonly HashSet<string> ClaimableDoorBuildingTypes = new() { "Shed", "Big Shed" };

	/// <summary>Get the lockable for a placed object, if it can be locked.</summary>
	/// <remarks>Player chests and big chests can be locked. Junimo chests share one inventory and mini shipping bins aren't storage, so they can't.</remarks>
	public static Lockable? FromObject(SObject? obj)
	{
		switch (obj)
		{
			case Chest chest when chest.playerChest.Value
				&& chest.SpecialChestType is Chest.SpecialChestTypes.None or Chest.SpecialChestTypes.BigChest:
				return new Lockable(chest, chest.modData, LockableKind.Chest);

			case StorageFurniture furniture:
				return new Lockable(furniture, furniture.modData, LockableKind.StorageFurniture);

			default:
				return null;
		}
	}

	/// <summary>Get the lockable for a building's front door, if it can be locked.</summary>
	/// <remarks>The farmhouse belongs to the host and each cabin to its resident. Sheds belong to whoever locks them first. Other buildings can't be locked.</remarks>
	public static Lockable? FromDoor(Building? building)
	{
		if (building is null || building.humanDoor.X < 0 || building.GetIndoors() is null)
			return null;

		if (building.GetIndoors() is FarmHouse house)
		{
			return house.HasOwner
				? new Lockable(building, building.modData, LockableKind.Door, fixedOwner: house.OwnerId)
				: null;
		}

		return ClaimableDoorBuildingTypes.Contains(building.buildingType.Value)
			? new Lockable(building, building.modData, LockableKind.Door)
			: null;
	}

	/// <summary>Whether a tile is the given building's front door.</summary>
	public static bool IsDoorTile(Building building, Vector2 tile)
	{
		Point door = building.getPointForHumanDoor();
		return (int)tile.X == door.X && (int)tile.Y == door.Y;
	}

	/// <summary>Get the lockable chest, storage furniture or door on a tile.</summary>
	public static Lockable? AtTile(GameLocation location, Vector2 tile)
	{
		if (location.Objects.TryGetValue(tile, out SObject? obj) && FromObject(obj) is { } container)
			return container;

		if (location.GetFurnitureAt(tile) is StorageFurniture furniture)
			return FromObject(furniture);

		Building? building = location.getBuildingAt(tile);
		return building is not null && IsDoorTile(building, tile)
			? FromDoor(building)
			: null;
	}

	/// <summary>Get the lockable chest or storage furniture opened by a menu, if any.</summary>
	/// <param name="menuSource">The object the menu was opened for, such as <c>ItemGrabMenu.context</c> or <c>ShopMenu.source</c>.</param>
	public static Lockable? FromMenuSource(object? menuSource)
	{
		return FromObject(menuSource as SObject);
	}
}
