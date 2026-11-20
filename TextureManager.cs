using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace LockChests;

internal static class TextureManager
{
	private static bool _initialized;

	private static readonly Rectangle KeySprite = new Rectangle(145, 322, 15, 14);

	private static readonly Rectangle XSprite = new Rectangle(269, 471, 14, 14);

	private static readonly Rectangle CrossSprite = new Rectangle(2, 412, 13, 13);

	private static readonly Rectangle StarSprite = new Rectangle(294, 392, 16, 16);

	private static readonly Rectangle HeartSprite = new Rectangle(211, 428, 7, 6);

	private static readonly Rectangle LockSprite = new Rectangle(107, 442, 7, 8);

	private static IMonitor? Monitor => ModEntry.IMonitor;

	public static void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			ModEntry.IHelper.Events.Display.RenderedWorld += OnRenderedWorld;
			IMonitor? monitor = Monitor;
			if (monitor != null)
			{
				monitor.Log("TextureManager initialized (sprite-only).", (LogLevel)0);
			}
		}
	}

	public static void RefreshTextures()
	{
	}

	public static void Cleanup()
	{
		try
		{
			if (ModEntry.IHelper != null)
			{
				ModEntry.IHelper.Events.Display.RenderedWorld -= OnRenderedWorld;
			}
		}
		catch
		{
		}
		_initialized = false;
	}

	private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
	{
		if (!Context.IsWorldReady || !ModEntry.IConfig.ShowLockIcons)
		{
			return;
		}
		try
		{
			GameLocation loc = Game1.currentLocation;
			if (((loc != null) ? loc.Objects : null) == null)
			{
				return;
			}
			foreach (KeyValuePair<Vector2, Object> pair in loc.Objects.Pairs)
			{
				Object value = pair.Value;
				Chest chest = (Chest)(object)((value is Chest) ? value : null);
				if (chest != null && ((NetFieldBase<bool, NetBool>)(object)chest.playerChest).Value)
				{
					bool isOwner = ModEntry.ParseOwner(chest) == Game1.player.UniqueMultiplayerID;
					if (ModEntry.ParseLock(chest) == Lock.Locked && (!isOwner || ModEntry.IConfig.ShowMyLockIcon) && (isOwner || ModEntry.IConfig.ShowOthersLockIcon))
					{
						DrawIconForChest(e.SpriteBatch, chest, isOwner);
					}
				}
			}
		}
		catch (Exception value2)
		{
			IMonitor? monitor = Monitor;
			if (monitor != null)
			{
				monitor.Log($"OnRenderedWorld error: {value2}", (LogLevel)0);
			}
		}
	}

	private static void DrawIconForChest(SpriteBatch b, Chest chest, bool isOwner)
	{
		Rectangle src = (isOwner ? GetSpriteRectangle(ModEntry.IConfig.OwnChestSprite, isOwnChest: true) : GetSpriteRectangle(ModEntry.IConfig.OtherChestSprite, isOwnChest: false));
		Color tint = Color.White;
		Vector2 tile = ((Object)chest).TileLocation;
		Vector2 chestTileWorld = default(Vector2);
		((Vector2)(ref chestTileWorld))._002Ector(tile.X * 64f, tile.Y * 64f);
		Vector2 baseWorld = Game1.GlobalToLocal(Game1.viewport, chestTileWorld);
		float targetPx = Math.Max(4f, 16f * Math.Max(0.1f, ModEntry.IConfig.LockSize));
		Rectangle dest = ComputeDestRect(baseWorld, (int)Math.Round(targetPx), (int)Math.Round(targetPx), ModEntry.IConfig.LockHorizontalPosition, ModEntry.IConfig.LockVerticalPosition);
		float layer = (tile.Y * 64f + 1f) / 10000f + 0.001f;
		int outline = Math.Max(0, (int)Math.Round(ModEntry.IConfig.OutlineThickness));
		if (outline > 0)
		{
			DrawOutlined(b, Game1.mouseCursors, src, dest, outline, layer);
		}
		b.Draw(Game1.mouseCursors, dest, (Rectangle?)src, tint, 0f, Vector2.Zero, (SpriteEffects)0, layer);
	}

	private static Rectangle ComputeDestRect(Vector2 chestTileWorld, int w, int h, int hPos, int vPos)
	{
		float x = hPos switch
		{
			0 => chestTileWorld.X + 8f, 
			1 => chestTileWorld.X + 32f - (float)w / 2f, 
			2 => chestTileWorld.X + 56f - (float)w, 
			_ => chestTileWorld.X + 32f - (float)w / 2f, 
		};
		float y = vPos switch
		{
			0 => chestTileWorld.Y - 64f + 8f, 
			1 => chestTileWorld.Y - 64f + 32f - (float)h / 2f, 
			2 => chestTileWorld.Y - 64f + 48f - (float)h, 
			3 => chestTileWorld.Y + 32f - (float)h / 2f, 
			4 => chestTileWorld.Y + 48f - (float)h, 
			_ => chestTileWorld.Y + 48f - (float)h, 
		};
		return new Rectangle((int)Math.Round(x), (int)Math.Round(y), w, h);
	}

	private static void DrawOutlined(SpriteBatch b, Texture2D tex, Rectangle src, Rectangle dest, int thickness, float layer)
	{
		Color outline = Color.Black;
		Rectangle odest = default(Rectangle);
		for (int dx = -thickness; dx <= thickness; dx++)
		{
			for (int dy = -thickness; dy <= thickness; dy++)
			{
				if (dx != 0 || dy != 0)
				{
					((Rectangle)(ref odest))._002Ector(dest.X + dx, dest.Y + dy, dest.Width, dest.Height);
					b.Draw(tex, odest, (Rectangle?)src, outline, 0f, Vector2.Zero, (SpriteEffects)0, layer - 0.0001f);
				}
			}
		}
	}

	private static Rectangle GetSpriteRectangle(string spriteName, bool isOwnChest)
	{
		return (Rectangle)(spriteName switch
		{
			"Key" => KeySprite, 
			"Lock" => LockSprite, 
			"Cross" => CrossSprite, 
			"Star" => StarSprite, 
			"Heart" => HeartSprite, 
			"X" => XSprite, 
			_ => isOwnChest ? StarSprite : XSprite, 
		});
	}
}
