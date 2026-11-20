using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace LockChests.Framework;

/// <summary>A safety net: closes any chest or storage menu that another mod opens for a player who's locked out.</summary>
/// <remarks>The game patches block normal opening, and the chest mod integrations hide locked chests, so this only catches other mods that open containers directly.</remarks>
internal static class MenuGuard
{
	public static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
	{
		object? source = e.NewMenu switch
		{
			ItemGrabMenu itemGrabMenu => itemGrabMenu.context,
			ShopMenu shopMenu => shopMenu.source,
			_ => null
		};

		Lockable? lockable = Lockables.FromMenuSource(source);
		if (!LockRules.IsLockedForLocalPlayer(lockable))
			return;

		e.NewMenu?.exitThisMenu(playSound: false);
		LockMessages.ShowLockedOut(lockable!);
	}
}
