using System;
using StardewModdingAPI;

namespace LockChests.Integrations;

/// <summary>Adds the mod's settings to Generic Mod Config Menu, if it's installed.</summary>
internal static class GenericModConfigMenuIntegration
{
	private const string ModId = "spacechase0.GenericModConfigMenu";

	private static readonly string[] IconChoices = { "Key", "Lock", "Cross", "Star", "Heart", "X" };
	private static readonly string[] HorizontalPositions = { "Left", "Center", "Right" };
	private static readonly string[] VerticalPositions = { "Above Lid", "Center of Lid", "Bottom of Lid", "Middle of Chest", "Bottom of Chest" };

	/// <summary>Register the settings.</summary>
	/// <param name="getConfig">Gets the current settings.</param>
	/// <param name="reset">Resets the settings to their defaults.</param>
	/// <param name="save">Saves the current settings.</param>
	public static void Register(IModHelper helper, IManifest manifest, Func<ModConfig> getConfig, Action reset, Action save)
	{
		IGenericModConfigMenuApi? api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ModId);
		if (api is null)
			return;

		api.Register(manifest, reset, save);

		api.AddSectionTitle(manifest, () => "Basic Settings");
		api.AddKeybind(manifest,
			getValue: () => getConfig().OpenKey,
			setValue: value => getConfig().OpenKey = value,
			name: () => "Lock/Unlock Key",
			tooltip: () => "Press while facing a chest, storage furniture or your house or shed door to lock or unlock it.");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().HostCanOpenEverything,
			setValue: value => getConfig().HostCanOpenEverything = value,
			name: () => "Host Can Open Everything",
			tooltip: () => "Lets the host open and unlock anything, so locks left by players who quit can be cleared. Off by default.");

		api.AddSectionTitle(manifest, () => "Lock Icons");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowLockIcons,
			setValue: value => getConfig().ShowLockIcons = value,
			name: () => "Show Lock Icons",
			tooltip: () => "Master toggle for all lock icons.");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowMyLockIcon,
			setValue: value => getConfig().ShowMyLockIcon = value,
			name: () => "  Icon on Mine",
			tooltip: () => "Show the icon on things you've locked.");
		api.AddTextOption(manifest,
			getValue: () => getConfig().OwnChestSprite,
			setValue: value => getConfig().OwnChestSprite = value,
			name: () => "  My Icon",
			tooltip: () => "The icon for things you've locked.",
			allowedValues: IconChoices);
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowOthersLockIcon,
			setValue: value => getConfig().ShowOthersLockIcon = value,
			name: () => "  Icon on Others'",
			tooltip: () => "Show the icon on things other players have locked.");
		api.AddTextOption(manifest,
			getValue: () => getConfig().OtherChestSprite,
			setValue: value => getConfig().OtherChestSprite = value,
			name: () => "  Others' Icon",
			tooltip: () => "The icon for things other players have locked.",
			allowedValues: IconChoices);
		api.AddNumberOption(manifest,
			getValue: () => getConfig().LockSize,
			setValue: value => getConfig().LockSize = value,
			name: () => "  Icon Size",
			tooltip: () => "Size multiplier.",
			min: 0.5f, max: 3f, interval: 0.25f);
		api.AddTextOption(manifest,
			getValue: () => HorizontalPositions[Math.Clamp(getConfig().LockHorizontalPosition, 0, HorizontalPositions.Length - 1)],
			setValue: value => getConfig().LockHorizontalPosition = Math.Max(0, Array.IndexOf(HorizontalPositions, value)),
			name: () => "  Horizontal Position",
			tooltip: () => "Where the icon sits across the chest.",
			allowedValues: HorizontalPositions);
		api.AddTextOption(manifest,
			getValue: () => VerticalPositions[Math.Clamp(getConfig().LockVerticalPosition, 0, VerticalPositions.Length - 1)],
			setValue: value => getConfig().LockVerticalPosition = Math.Max(0, Array.IndexOf(VerticalPositions, value)),
			name: () => "  Vertical Position",
			tooltip: () => "Where the icon sits on chests. Furniture and doors always show it above them.",
			allowedValues: VerticalPositions);

		api.AddSectionTitle(manifest, () => "Hover Text");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowHoverText,
			setValue: value => getConfig().ShowHoverText = value,
			name: () => "Show Hover Text",
			tooltip: () => "Master toggle: show the owner and lock state when you point at an owned chest, storage furniture or door.");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowMyChestHoverText,
			setValue: value => getConfig().ShowMyChestHoverText = value,
			name: () => "  On Mine",
			tooltip: () => "Show hover text on things you own.");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowOthersChestHoverText,
			setValue: value => getConfig().ShowOthersChestHoverText = value,
			name: () => "  On Others'",
			tooltip: () => "Show hover text on things other players own.");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowOwnerInHoverText,
			setValue: value => getConfig().ShowOwnerInHoverText = value,
			name: () => "  Show Owner",
			tooltip: () => "Include the owner's name.");
		api.AddBoolOption(manifest,
			getValue: () => getConfig().ShowStatusInHoverText,
			setValue: value => getConfig().ShowStatusInHoverText = value,
			name: () => "  Show Lock State",
			tooltip: () => "Include whether it's locked or open.");
	}
}
