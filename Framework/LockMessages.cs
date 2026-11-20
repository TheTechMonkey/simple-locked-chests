using StardewValley;

namespace LockChests.Framework;

/// <summary>Builds and shows the messages players see.</summary>
internal static class LockMessages
{
	/// <summary>The minimum time between repeated denial messages, so holding a button doesn't flood the screen.</summary>
	private const int DenialCooldownMilliseconds = 1500;

	/// <summary>When the last denial message was shown, in game milliseconds.</summary>
	private static double lastDenialShownAt = double.MinValue;

	/// <summary>The translated noun for the lockable's kind, like "chest" or "door".</summary>
	public static string GetThingName(Lockable lockable)
	{
		return lockable.Kind switch
		{
			LockableKind.Door => I18n.Get("Thing_Door", "door"),
			LockableKind.StorageFurniture => I18n.Get("Thing_Storage", "storage"),
			_ => I18n.Get("Thing_Chest", "chest")
		};
	}

	/// <summary>The translated lock state.</summary>
	public static string GetStateName(LockState state)
	{
		return state == LockState.Locked
			? I18n.Get("Status_Locked", "Locked")
			: I18n.Get("Status_Open", "Open");
	}

	/// <summary>Show that the local player can't use something because its owner locked it.</summary>
	public static void ShowLockedOut(Lockable lockable)
	{
		ShowDenial(
			Format("No_Access", "{{Name}} is the owner and has locked this {{Thing}}.", lockable)
		);
	}

	/// <summary>Show that the local player can't lock or unlock something they don't own.</summary>
	public static void ShowNotOwner(Lockable lockable)
	{
		ShowDenial(
			Format("Not_Owner", "{{Name}} owns this {{Thing}}. Only they can lock or unlock it.", lockable)
		);
	}

	/// <summary>Get a translated message with the owner's name and the thing's kind filled in.</summary>
	public static string Format(string key, string fallback, Lockable lockable)
	{
		string ownerName = LockRules.GetOwnerName(lockable.Owner);
		string thing = GetThingName(lockable);
		var tokens = new { Name = ownerName, Thing = thing };
		return I18n.Get(key, fallback.Replace("{{Name}}", ownerName).Replace("{{Thing}}", thing), tokens);
	}

	/// <summary>Show a red denial message with a sound, unless one was just shown.</summary>
	private static void ShowDenial(string message)
	{
		double now = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
		if (now - lastDenialShownAt < DenialCooldownMilliseconds)
			return;

		lastDenialShownAt = now;
		Game1.playSound("cancel");
		Game1.showRedMessage(message, playSound: false);
	}
}
