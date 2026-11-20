using StardewModdingAPI;

namespace LockChests;

public class Config
{
	public SButton OpenKey { get; set; } = (SButton)120;

	public bool FilterThirdPartyMods { get; set; } = true;

	public int UpdateFrequency { get; set; } = 60;

	public bool ShowLockIcons { get; set; } = true;

	public bool ShowHoverText { get; set; } = true;

	public bool ShowMyLockIcon { get; set; } = true;

	public bool ShowOthersLockIcon { get; set; } = true;

	public bool ShowMyChestHoverText { get; set; } = true;

	public bool ShowOthersChestHoverText { get; set; } = true;

	public bool ShowOwnerInHoverText { get; set; } = true;

	public bool ShowStatusInHoverText { get; set; } = true;

	public bool ShowLockIcon { get; set; } = true;

	public bool ShowOwnerOnHover { get; set; } = true;

	public bool ShowStatusOnHover { get; set; } = true;

	public string OwnChestSprite { get; set; } = "Star";

	public string OtherChestSprite { get; set; } = "X";

	public float LockSize { get; set; } = 0.5f;

	public float OutlineThickness { get; set; } = 1f;

	public int LockHorizontalPosition { get; set; } = 1;

	public int LockVerticalPosition { get; set; } = 4;

	public bool DrawOnNorthTile { get; set; } = true;

	public int LockIconShape { get; set; } = 3;

	public int OtherChestIconShape { get; set; } = 15;
}
