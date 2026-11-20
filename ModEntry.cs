using HarmonyLib;
using LockChests.Framework;
using LockChests.Integrations;
using LockChests.Patches;
using LockChests.Rendering;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LockChests;

/// <summary>The mod entry point.</summary>
public class ModEntry : Mod
{
	private ModConfig config = new();

	public override void Entry(IModHelper helper)
	{
		this.config = helper.ReadConfig<ModConfig>();
		I18n.Init(helper.Translation);
		LockRules.Init(() => this.config.HostCanOpenEverything);

		var harmony = new Harmony(this.ModManifest.UniqueID);
		GamePatches.Apply(harmony, this.Monitor);

		var lockKeyHandler = new LockKeyHandler(() => this.config, this.Monitor);
		var lockIconRenderer = new LockIconRenderer(() => this.config, this.Monitor);
		var hoverTextRenderer = new HoverTextRenderer(() => this.config, this.Monitor);

		helper.Events.GameLoop.GameLaunched += (_, _) => this.OnGameLaunched(harmony);
		helper.Events.Input.ButtonPressed += lockKeyHandler.OnButtonPressed;
		helper.Events.Display.MenuChanged += MenuGuard.OnMenuChanged;
		helper.Events.Display.RenderedWorld += lockIconRenderer.OnRenderedWorld;
		helper.Events.Display.RenderedHud += hoverTextRenderer.OnRenderedHud;
	}

	/// <summary>Set up integrations once every mod has loaded.</summary>
	private void OnGameLaunched(Harmony harmony)
	{
		ChestsAnywhereIntegration.Apply(harmony, this.Helper.ModRegistry, this.Monitor);
		AllChestsMenuIntegration.Apply(harmony, this.Helper.ModRegistry, this.Monitor);
		GenericModConfigMenuIntegration.Register(
			this.Helper,
			this.ModManifest,
			getConfig: () => this.config,
			reset: () => this.config = new ModConfig(),
			save: () => this.Helper.WriteConfig(this.config)
		);
	}
}
