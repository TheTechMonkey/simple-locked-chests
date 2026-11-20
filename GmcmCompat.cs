using System;
using StardewModdingAPI;

namespace LockChests;

internal static class GmcmCompat
{
	private static readonly string[] SpriteChoices = new string[6] { "Key", "Lock", "Cross", "Star", "Heart", "X" };

	public static void Hook(IModHelper helper, IManifest manifest, Func<Config> getConfig, Action<Config> setAndSave)
	{
		IGenericModConfigMenuApi api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
		if (api == null)
		{
			IMonitor? iMonitor = ModEntry.IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log("GMCM not found, skipping config UI", (LogLevel)1);
			}
			return;
		}
		api.Register(manifest, delegate
		{
			setAndSave(new Config());
		}, delegate
		{
			setAndSave(getConfig());
		});
		api.AddSectionTitle(manifest, () => "Basic Settings");
		api.AddKeybind(manifest, () => getConfig().OpenKey, delegate(SButton value)
		{
			Config config = getConfig();
			config.OpenKey = value;
			setAndSave(config);
		}, () => "Lock/Unlock Key", () => "Key to toggle chest lock");
		api.AddBoolOption(manifest, () => getConfig().FilterThirdPartyMods, delegate(bool value)
		{
			Config config = getConfig();
			config.FilterThirdPartyMods = value;
			setAndSave(config);
		}, () => "Hide from Other Mods", () => "Hide locked chests from Chests Anywhere/All Chests Menu");
		api.AddNumberOption(manifest, () => getConfig().UpdateFrequency, delegate(int value)
		{
			Config config = getConfig();
			config.UpdateFrequency = Math.Max(30, value);
			setAndSave(config);
		}, () => "Update Frequency", () => "How often to rebuild the chest index (in game ticks, 60 = 1 second). Lower = more responsive but uses more CPU. Default: 60", 30, 300, 30);
		api.AddSectionTitle(manifest, () => "Lock Icons");
		api.AddBoolOption(manifest, () => getConfig().ShowLockIcons, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowLockIcons = value;
			setAndSave(config);
		}, () => "Show Lock Icons", () => "Master toggle for all lock icons");
		api.AddParagraph(manifest, () => "--- My Chests ---");
		api.AddBoolOption(manifest, () => getConfig().ShowMyLockIcon, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowMyLockIcon = value;
			setAndSave(config);
		}, () => "  Show Icon on My Chests", () => "Show lock icon on chests you own");
		api.AddTextOption(manifest, () => getConfig().OwnChestSprite, delegate(string value)
		{
			Config config = getConfig();
			config.OwnChestSprite = value;
			setAndSave(config);
		}, () => "  My Chest Icon", () => "Icon for your locked chests", SpriteChoices);
		api.AddParagraph(manifest, () => "--- Other Players' Chests ---");
		api.AddBoolOption(manifest, () => getConfig().ShowOthersLockIcon, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowOthersLockIcon = value;
			setAndSave(config);
		}, () => "  Show Icon on Others' Chests", () => "Show lock icon on chests owned by others");
		api.AddTextOption(manifest, () => getConfig().OtherChestSprite, delegate(string value)
		{
			Config config = getConfig();
			config.OtherChestSprite = value;
			setAndSave(config);
		}, () => "  Others' Chest Icon", () => "Icon for other players' locked chests", SpriteChoices);
		api.AddParagraph(manifest, () => "--- Icon Appearance ---");
		api.AddNumberOption(manifest, () => getConfig().LockSize, delegate(float value)
		{
			Config config = getConfig();
			config.LockSize = value;
			setAndSave(config);
		}, () => "  Icon Size", () => "Size multiplier", 0.5f, 3f, 0.25f);
		api.AddTextOption(manifest, () => PositionToString(getConfig().LockHorizontalPosition, isHorizontal: true), delegate(string value)
		{
			Config config = getConfig();
			config.LockHorizontalPosition = StringToPosition(value, isHorizontal: true);
			setAndSave(config);
		}, () => "  Horizontal Position", () => "Icon horizontal placement", new string[3] { "Left", "Center", "Right" });
		api.AddTextOption(manifest, () => PositionToString(getConfig().LockVerticalPosition, isHorizontal: false), delegate(string value)
		{
			Config config = getConfig();
			config.LockVerticalPosition = StringToPosition(value, isHorizontal: false);
			setAndSave(config);
		}, () => "  Vertical Position", () => "Icon vertical placement relative to chest", new string[5] { "Above Lid", "Center of Lid", "Bottom of Lid", "Middle of Chest", "Bottom of Chest" });
		api.AddSectionTitle(manifest, () => "Hover Text");
		api.AddBoolOption(manifest, () => getConfig().ShowHoverText, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowHoverText = value;
			setAndSave(config);
		}, () => "Show Hover Text", () => "Master toggle for all hover text");
		api.AddParagraph(manifest, () => "--- What to Show ---");
		api.AddBoolOption(manifest, () => getConfig().ShowMyChestHoverText, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowMyChestHoverText = value;
			setAndSave(config);
		}, () => "  Text on My Chests", () => "Show hover text on your chests");
		api.AddBoolOption(manifest, () => getConfig().ShowOthersChestHoverText, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowOthersChestHoverText = value;
			setAndSave(config);
		}, () => "  Text on Others' Chests", () => "Show hover text on other players' chests");
		api.AddBoolOption(manifest, () => getConfig().ShowOwnerInHoverText, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowOwnerInHoverText = value;
			setAndSave(config);
		}, () => "  Show Owner Name", () => "Display chest owner in hover text");
		api.AddBoolOption(manifest, () => getConfig().ShowStatusInHoverText, delegate(bool value)
		{
			Config config = getConfig();
			config.ShowStatusInHoverText = value;
			setAndSave(config);
		}, () => "  Show Lock Status", () => "Display locked/unlocked status");
		IMonitor? iMonitor2 = ModEntry.IMonitor;
		if (iMonitor2 != null)
		{
			iMonitor2.Log("GMCM integration successful", (LogLevel)1);
		}
	}

	private static string PositionToString(int pos, bool isHorizontal)
	{
		if (isHorizontal)
		{
			return pos switch
			{
				0 => "Left", 
				2 => "Right", 
				_ => "Center", 
			};
		}
		return pos switch
		{
			0 => "Above Lid", 
			1 => "Center of Lid", 
			2 => "Bottom of Lid", 
			3 => "Middle of Chest", 
			4 => "Bottom of Chest", 
			_ => "Bottom of Chest", 
		};
	}

	private static int StringToPosition(string str, bool isHorizontal)
	{
		if (isHorizontal)
		{
			return (!(str == "Left")) ? ((!(str == "Right")) ? 1 : 2) : 0;
		}
		return str switch
		{
			"Above Lid" => 0, 
			"Center of Lid" => 1, 
			"Bottom of Lid" => 2, 
			"Middle of Chest" => 3, 
			"Bottom of Chest" => 4, 
			_ => 4, 
		};
	}
}
