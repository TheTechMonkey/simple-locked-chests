using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace LockChests;

internal static class ChestIndex
{
	private static readonly Dictionary<object, Chest> _byItemsRef = new Dictionary<object, Chest>();

	private static bool _rebuildScheduled = false;

	private static readonly object _indexLock = new object();

	public static void ScheduleRebuild()
	{
		_rebuildScheduled = true;
	}

	public static void ProcessScheduledRebuild()
	{
		if (_rebuildScheduled)
		{
			RebuildIndex();
			_rebuildScheduled = false;
		}
	}

	public static void RebuildIndex()
	{
		try
		{
			lock (_indexLock)
			{
				_byItemsRef.Clear();
				int chestCount = 0;
				foreach (GameLocation location in EnumerateAllLocations())
				{
					chestCount += IndexLocationChests(location);
				}
				IMonitor? iMonitor = ModEntry.IMonitor;
				if (iMonitor != null)
				{
					iMonitor.Log($"Chest index rebuilt with {chestCount} entries", (LogLevel)0);
				}
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor2 = ModEntry.IMonitor;
			if (iMonitor2 != null)
			{
				iMonitor2.Log($"Failed to rebuild chest index: {value}", (LogLevel)4);
			}
		}
	}

	private static int IndexLocationChests(GameLocation location)
	{
		if (((location != null) ? location.Objects : null) == null)
		{
			return 0;
		}
		int count = 0;
		foreach (KeyValuePair<Vector2, Object> pair in location.Objects.Pairs)
		{
			try
			{
				Object value = pair.Value;
				Chest chest = (Chest)(object)((value is Chest) ? value : null);
				if (chest != null && ((NetFieldBase<bool, NetBool>)(object)chest.playerChest).Value && chest.Items != null)
				{
					object key = chest.Items;
					if (!_byItemsRef.ContainsKey(key))
					{
						_byItemsRef[key] = chest;
						count++;
					}
				}
			}
			catch (Exception value2)
			{
				IMonitor? iMonitor = ModEntry.IMonitor;
				if (iMonitor != null)
				{
					iMonitor.Log($"Error indexing chest at {pair.Key}: {value2}", (LogLevel)3);
				}
			}
		}
		return count;
	}

	public static Chest? TryFindByItemsList(object itemsListRef)
	{
		if (itemsListRef == null)
		{
			return null;
		}
		lock (_indexLock)
		{
			Chest chest;
			return _byItemsRef.TryGetValue(itemsListRef, out chest) ? chest : null;
		}
	}

	private static IEnumerable<GameLocation> EnumerateAllLocations()
	{
		List<GameLocation> locations = new List<GameLocation>();
		try
		{
			if (Game1.locations != null)
			{
				foreach (GameLocation location in Game1.locations)
				{
					if (location == null)
					{
						continue;
					}
					locations.Add(location);
					foreach (Building item in GetBuildingsSafely(location))
					{
						GameLocation indoors = ((NetFieldBase<GameLocation, NetRef<GameLocation>>)(object)item?.indoors)?.Value;
						if (indoors != null)
						{
							locations.Add(indoors);
						}
					}
				}
			}
			if (Game1.currentLocation != null && !locations.Contains(Game1.currentLocation))
			{
				locations.Add(Game1.currentLocation);
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error enumerating locations: {value}", (LogLevel)3);
			}
		}
		return locations;
	}

	private static IEnumerable<Building> GetBuildingsSafely(GameLocation? location)
	{
		if (location == null)
		{
			return Array.Empty<Building>();
		}
		try
		{
			if (((object)location).GetType().GetProperty("buildings")?.GetValue(location) is IEnumerable<Building> buildings)
			{
				return buildings.Where((Building b) => b != null);
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"Error getting buildings from {location.Name}: {value}", (LogLevel)2);
			}
		}
		return Array.Empty<Building>();
	}

	public static int GetIndexSize()
	{
		lock (_indexLock)
		{
			return _byItemsRef.Count;
		}
	}

	public static void ClearIndex()
	{
		lock (_indexLock)
		{
			_byItemsRef.Clear();
			_rebuildScheduled = false;
		}
	}
}
