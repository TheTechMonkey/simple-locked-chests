using StardewModdingAPI;

namespace LockChests;

/// <summary>The mod settings, saved in <c>config.json</c>.</summary>
/// <remarks>Property names are unchanged from earlier versions so existing config files keep their values.</remarks>
public class ModConfig
{
	/// <summary>The key that locks or unlocks the chest, storage furniture or door you're facing.</summary>
	public SButton OpenKey { get; set; } = SButton.F9;

	/// <summary>Whether the host can open and unlock everything, so locks left by players who quit can be cleared.</summary>
	public bool HostCanOpenEverything { get; set; } = false;

	/// <summary>Whether lock icons are drawn at all.</summary>
	public bool ShowLockIcons { get; set; } = true;

	/// <summary>Whether lock icons are drawn on things you own.</summary>
	public bool ShowMyLockIcon { get; set; } = true;

	/// <summary>Whether lock icons are drawn on things other players own.</summary>
	public bool ShowOthersLockIcon { get; set; } = true;

	/// <summary>The icon for things you own: Key, Lock, Cross, Star, Heart or X.</summary>
	public string OwnChestSprite { get; set; } = "Star";

	/// <summary>The icon for things other players own: Key, Lock, Cross, Star, Heart or X.</summary>
	public string OtherChestSprite { get; set; } = "X";

	/// <summary>The icon size multiplier.</summary>
	public float LockSize { get; set; } = 0.5f;

	/// <summary>The black outline thickness around icons, in pixels. 0 turns it off.</summary>
	public float OutlineThickness { get; set; } = 1f;

	/// <summary>Icon horizontal position: 0 = left, 1 = center, 2 = right.</summary>
	public int LockHorizontalPosition { get; set; } = 1;

	/// <summary>Icon vertical position on chests: 0 = above lid, 1 = center of lid, 2 = bottom of lid, 3 = middle of chest, 4 = bottom of chest.</summary>
	public int LockVerticalPosition { get; set; } = 4;

	/// <summary>Whether hover text is shown at all.</summary>
	public bool ShowHoverText { get; set; } = true;

	/// <summary>Whether hover text is shown on things you own.</summary>
	public bool ShowMyChestHoverText { get; set; } = true;

	/// <summary>Whether hover text is shown on things other players own.</summary>
	public bool ShowOthersChestHoverText { get; set; } = true;

	/// <summary>Whether hover text includes the owner's name.</summary>
	public bool ShowOwnerInHoverText { get; set; } = true;

	/// <summary>Whether hover text includes whether it's locked.</summary>
	public bool ShowStatusInHoverText { get; set; } = true;
}
