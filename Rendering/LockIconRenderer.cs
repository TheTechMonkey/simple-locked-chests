using System;
using LockChests.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace LockChests.Rendering;

/// <summary>Draws a lock icon over locked chests, storage furniture and doors in the current location.</summary>
internal sealed class LockIconRenderer
{
	private static readonly Rectangle KeySprite = new(145, 322, 15, 14);
	private static readonly Rectangle XSprite = new(269, 471, 14, 14);
	private static readonly Rectangle CrossSprite = new(2, 412, 13, 13);
	private static readonly Rectangle StarSprite = new(294, 392, 16, 16);
	private static readonly Rectangle HeartSprite = new(211, 428, 7, 6);
	private static readonly Rectangle LockSprite = new(107, 442, 7, 8);

	private readonly Func<ModConfig> getConfig;
	private readonly IMonitor monitor;
	private bool loggedDrawError;

	public LockIconRenderer(Func<ModConfig> getConfig, IMonitor monitor)
	{
		this.getConfig = getConfig;
		this.monitor = monitor;
	}

	/// <summary>Draw icons after the world is drawn.</summary>
	public void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
	{
		ModConfig config = this.getConfig();
		GameLocation? location = Game1.currentLocation;
		if (!Context.IsWorldReady || !config.ShowLockIcons || location is null)
			return;

		try
		{
			foreach (SObject obj in location.Objects.Values)
			{
				if (obj is Chest && Lockables.FromObject(obj) is { } chest)
					this.DrawIfLocked(e.SpriteBatch, chest, ChestIconArea(obj.TileLocation), config, useChestPositions: true);
			}

			foreach (Furniture furniture in location.furniture)
			{
				if (furniture is StorageFurniture && Lockables.FromObject(furniture) is { } storage)
					this.DrawIfLocked(e.SpriteBatch, storage, FurnitureIconArea(furniture), config, useChestPositions: false);
			}

			foreach (Building building in location.buildings)
			{
				if (Lockables.FromDoor(building) is { } door)
					this.DrawIfLocked(e.SpriteBatch, door, DoorIconArea(building), config, useChestPositions: false);
			}
		}
		catch (Exception ex)
		{
			if (!this.loggedDrawError)
			{
				this.monitor.Log($"Failed drawing lock icons: {ex}", LogLevel.Error);
				this.loggedDrawError = true;
			}
		}
	}

	/// <summary>Draw the icon if the lockable is locked and icons are enabled for its owner.</summary>
	/// <param name="area">The world-pixel area the icon is positioned in.</param>
	private void DrawIfLocked(SpriteBatch spriteBatch, Lockable lockable, Rectangle area, ModConfig config, bool useChestPositions)
	{
		if (lockable.State != LockState.Locked || lockable.Owner == LockData.NoOwner)
			return;

		bool isMine = LockRules.IsOwnerOrSpouse(lockable, Game1.player);
		if (isMine ? !config.ShowMyLockIcon : !config.ShowOthersLockIcon)
			return;

		Rectangle sprite = GetSprite(isMine ? config.OwnChestSprite : config.OtherChestSprite, isMine);
		int size = (int)Math.Round(Math.Max(4f, 16f * Math.Max(0.1f, config.LockSize)));
		Rectangle worldDest = useChestPositions
			? GetChestIconRect(area, size, config.LockHorizontalPosition, config.LockVerticalPosition)
			: GetTopIconRect(area, size, config.LockHorizontalPosition);
		Vector2 screenPosition = Game1.GlobalToLocal(Game1.viewport, new Vector2(worldDest.X, worldDest.Y));
		var dest = new Rectangle((int)screenPosition.X, (int)screenPosition.Y, worldDest.Width, worldDest.Height);
		float layerDepth = (area.Bottom + 1) / 10000f + 0.001f;

		int outline = Math.Max(0, (int)Math.Round(config.OutlineThickness));
		for (int dx = -outline; dx <= outline; dx++)
		{
			for (int dy = -outline; dy <= outline; dy++)
			{
				if (dx != 0 || dy != 0)
					spriteBatch.Draw(Game1.mouseCursors, new Rectangle(dest.X + dx, dest.Y + dy, dest.Width, dest.Height), sprite, Color.Black, 0f, Vector2.Zero, SpriteEffects.None, layerDepth - 0.0001f);
			}
		}
		spriteBatch.Draw(Game1.mouseCursors, dest, sprite, Color.White, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
	}

	/// <summary>The chest's tile, in world pixels.</summary>
	private static Rectangle ChestIconArea(Vector2 tile)
	{
		return new Rectangle((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
	}

	/// <summary>The furniture's visible sprite area, in world pixels.</summary>
	private static Rectangle FurnitureIconArea(Furniture furniture)
	{
		Rectangle footprint = furniture.GetBoundingBox();
		int spriteHeight = furniture.sourceRect.Value.Height * Game1.pixelZoom;
		int spriteTop = footprint.Bottom - Math.Max(spriteHeight, footprint.Height);
		return new Rectangle(footprint.X, spriteTop, footprint.Width, footprint.Bottom - spriteTop);
	}

	/// <summary>The building's front door tile, in world pixels.</summary>
	private static Rectangle DoorIconArea(Building building)
	{
		Point door = building.getPointForHumanDoor();
		return new Rectangle(door.X * Game1.tileSize, door.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
	}

	/// <summary>Position an icon on a chest using the configured horizontal and vertical positions.</summary>
	private static Rectangle GetChestIconRect(Rectangle tile, int size, int horizontal, int vertical)
	{
		float x = GetHorizontalX(tile, size, horizontal);
		float y = vertical switch
		{
			0 => tile.Y - 64f + 8f,
			1 => tile.Y - 64f + 32f - size / 2f,
			2 => tile.Y - 64f + 48f - size,
			3 => tile.Y + 32f - size / 2f,
			_ => tile.Y + 48f - size
		};
		return new Rectangle((int)Math.Round(x), (int)Math.Round(y), size, size);
	}

	/// <summary>Position an icon near the top of furniture or a door.</summary>
	private static Rectangle GetTopIconRect(Rectangle area, int size, int horizontal)
	{
		float x = GetHorizontalX(area, size, horizontal);
		return new Rectangle((int)Math.Round(x), area.Y + 8, size, size);
	}

	private static float GetHorizontalX(Rectangle area, int size, int horizontal)
	{
		return horizontal switch
		{
			0 => area.X + 8f,
			2 => area.Right - 8f - size,
			_ => area.X + area.Width / 2f - size / 2f
		};
	}

	private static Rectangle GetSprite(string name, bool isMine)
	{
		return name switch
		{
			"Key" => KeySprite,
			"Lock" => LockSprite,
			"Cross" => CrossSprite,
			"Star" => StarSprite,
			"Heart" => HeartSprite,
			"X" => XSprite,
			_ => isMine ? StarSprite : XSprite
		};
	}
}
