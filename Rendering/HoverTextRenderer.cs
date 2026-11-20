using System;
using System.Collections.Generic;
using LockChests.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace LockChests.Rendering;

/// <summary>Shows the owner and lock state when the cursor is over an owned chest, storage furniture or locked door.</summary>
internal sealed class HoverTextRenderer
{
	private readonly Func<ModConfig> getConfig;
	private readonly IMonitor monitor;
	private bool loggedDrawError;

	public HoverTextRenderer(Func<ModConfig> getConfig, IMonitor monitor)
	{
		this.getConfig = getConfig;
		this.monitor = monitor;
	}

	/// <summary>Draw hover text after the HUD is drawn.</summary>
	public void OnRenderedHud(object? sender, RenderedHudEventArgs e)
	{
		ModConfig config = this.getConfig();
		if (!config.ShowHoverText || !Context.IsPlayerFree || Game1.currentLocation is null)
			return;

		try
		{
			Lockable? lockable = FindUnderCursor(Game1.currentLocation);
			string? text = lockable is null ? null : BuildText(lockable, config);
			if (text is not null)
				IClickableMenu.drawHoverText(e.SpriteBatch, text, Game1.smallFont);
		}
		catch (Exception ex)
		{
			if (!this.loggedDrawError)
			{
				this.monitor.Log($"Failed drawing hover text: {ex}", LogLevel.Error);
				this.loggedDrawError = true;
			}
		}
	}

	/// <summary>Find the lockable under the cursor. A chest's lid covers the tile above it, so that tile counts too.</summary>
	private static Lockable? FindUnderCursor(GameLocation location)
	{
		Vector2 tile = Game1.currentCursorTile;
		Lockable? lockable = Lockables.AtTile(location, tile);
		if (lockable is null && Lockables.AtTile(location, tile + new Vector2(0, 1)) is { Kind: LockableKind.Chest } chestBelow)
			lockable = chestBelow;
		return lockable;
	}

	/// <summary>Build the hover text, or null if nothing should be shown.</summary>
	private static string? BuildText(Lockable lockable, ModConfig config)
	{
		if (lockable.Owner == LockData.NoOwner)
			return null;

		// house doors always have an owner, so only point them out when they're locked
		if (lockable.Kind == LockableKind.Door && lockable.State != LockState.Locked)
			return null;

		bool isMine = lockable.IsOwnedBy(Game1.player);
		if (isMine ? !config.ShowMyChestHoverText : !config.ShowOthersChestHoverText)
			return null;

		var lines = new List<string>();
		if (config.ShowOwnerInHoverText)
			lines.Add($"{I18n.Get("Owner_Label", "Owner")}: {LockRules.GetOwnerName(lockable.Owner)}");
		if (config.ShowStatusInHoverText)
			lines.Add($"{I18n.Get("Status_Label", "Lock")}: {LockMessages.GetStateName(lockable.State)}");

		return lines.Count > 0 ? string.Join("\n", lines) : null;
	}
}
