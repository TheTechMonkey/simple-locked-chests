using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace LockChests;

public class ModEntry : Mod
{
	internal static IMonitor? IMonitor;

	internal static IModHelper? IHelper;

	internal static IManifest? Manifest;

	internal static Config IConfig = new Config();

	private static readonly object ModDataLock = new object();

	private static bool _isFilteringActive;

	private static int _filterTickCounter;

	internal static ITranslationHelper? ITranslations
	{
		get
		{
			IModHelper? iHelper = IHelper;
			if (iHelper == null)
			{
				return null;
			}
			return iHelper.Translation;
		}
	}

	public override void Entry(IModHelper helper)
	{
		IMonitor = ((Mod)this).Monitor;
		IHelper = helper;
		Manifest = ((Mod)this).ModManifest;
		IConfig = helper.ReadConfig<Config>();
		helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
		helper.Events.GameLoop.DayStarted += OnDayStarted;
		helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		helper.Events.Player.Warped += OnWarped;
		helper.Events.World.ObjectListChanged += OnObjectListChanged;
		helper.Events.Input.ButtonPressed += OnButtonPressed;
		ApplyPatchesSafely();
	}

	private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
	{
		try
		{
			GmcmCompat.Hook(IHelper, Manifest, () => IConfig, delegate(Config cfg)
			{
				IConfig = cfg;
				IHelper.WriteConfig<Config>(IConfig);
			});
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"GMCM hook failed: {value}", (LogLevel)0);
			}
		}
	}

	private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
	{
		try
		{
			ChestIndex.ScheduleRebuild();
			BeginQuickFilterBurst();
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"OnSaveLoaded error: {value}", (LogLevel)4);
			}
		}
	}

	private void OnDayStarted(object? sender, DayStartedEventArgs e)
	{
		try
		{
			ChestIndex.ScheduleRebuild();
			BeginQuickFilterBurst();
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"OnDayStarted error: {value}", (LogLevel)4);
			}
		}
	}

	private void OnWarped(object? sender, WarpedEventArgs e)
	{
		try
		{
			if (e.IsLocalPlayer)
			{
				ChestIndex.ScheduleRebuild();
				BeginQuickFilterBurst();
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"OnWarped error: {value}", (LogLevel)4);
			}
		}
	}

	private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
	{
		try
		{
			ChestIndex.ScheduleRebuild();
			if (IConfig.FilterThirdPartyMods && _isFilteringActive)
			{
				IClickableMenu menu = Game1.activeClickableMenu;
				if (menu != null && IntegrationFilters.IsThirdPartyChestBrowser(menu))
				{
					IntegrationFilters.FilterActiveThirdPartyMenu();
				}
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"OnObjectListChanged error: {value}", (LogLevel)4);
			}
		}
	}

	private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
	{
		try
		{
			int freq = Math.Max(30, IConfig.UpdateFrequency);
			if (e.IsMultipleOf((uint)freq))
			{
				ChestIndex.ProcessScheduledRebuild();
			}
			if (_isFilteringActive)
			{
				_filterTickCounter++;
				IClickableMenu menu = Game1.activeClickableMenu;
				if (menu != null && IntegrationFilters.IsThirdPartyChestBrowser(menu))
				{
					IntegrationFilters.FilterActiveThirdPartyMenu();
					IntegrationFilters.HideLockedChestContentsIfNonOwner();
				}
				if (_filterTickCounter > 60)
				{
					_isFilteringActive = false;
				}
			}
			else if (IConfig.FilterThirdPartyMods && e.IsMultipleOf(120u))
			{
				IClickableMenu menu2 = Game1.activeClickableMenu;
				if (menu2 != null && IntegrationFilters.IsThirdPartyChestBrowser(menu2))
				{
					IntegrationFilters.FilterActiveThirdPartyMenu();
				}
				IntegrationFilters.HideLockedChestContentsIfNonOwner();
			}
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"OnUpdateTicked error: {value}", (LogLevel)4);
			}
		}
	}

	private void BeginQuickFilterBurst()
	{
		_isFilteringActive = true;
		_filterTickCounter = 0;
	}

	private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
	{
		if (!Context.IsWorldReady || !Context.CanPlayerMove || e.Button != IConfig.OpenKey)
		{
			return;
		}
		try
		{
			Chest chest = GetTargetChest();
			if (chest == null)
			{
				Game1.playSound("cancel", (int?)null);
				return;
			}
			long owner = ParseOwner(chest);
			Lock lockState = ParseLock(chest);
			if (lockState == Lock.Locked && owner != -1 && owner != Game1.player.UniqueMultiplayerID)
			{
				ShowAccessDeniedMessage(owner);
				return;
			}
			if (owner == -1)
			{
				AssignOwnership(chest);
			}
			ShowLockManagementDialog(chest);
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"ButtonPressed error: {value}", (LogLevel)4);
			}
			Game1.playSound("cancel", (int?)null);
			ITranslationHelper? iTranslations = ITranslations;
			Translation val = ((iTranslations != null) ? iTranslations.Get("Error_Managing_Lock") : null);
			Game1.drawObjectDialogue((val != null) ? Translation.op_Implicit(val) : "Error managing lock.");
		}
	}

	private Chest? GetTargetChest()
	{
		Vector2 tile = ((Character)Game1.player).GetGrabTile();
		Object obj = default(Object);
		if (Game1.currentLocation.Objects.TryGetValue(tile, ref obj))
		{
			Chest chest = (Chest)(object)((obj is Chest) ? obj : null);
			if (chest != null && ((NetFieldBase<bool, NetBool>)(object)chest.playerChest).Value)
			{
				return chest;
			}
		}
		return null;
	}

	private void ShowAccessDeniedMessage(long ownerId)
	{
		Farmer ownerFarmer = TryGetPlayer(ownerId);
		object obj = ((ownerFarmer != null) ? ((Character)ownerFarmer).Name : null);
		Translation val;
		if (obj == null)
		{
			ITranslationHelper? iTranslations = ITranslations;
			val = ((iTranslations != null) ? iTranslations.Get("Unknown_Player") : null);
			obj = ((val != null) ? Translation.op_Implicit(val) : "Another player");
		}
		string ownerName = (string)obj;
		ITranslationHelper? iTranslations2 = ITranslations;
		val = ((iTranslations2 != null) ? iTranslations2.Get("No_Access_Dialogue", (object)new
		{
			Name = ownerName
		}) : null);
		string message = ((val != null) ? Translation.op_Implicit(val) : ("You can't access " + ownerName + "'s chest.")).ToString();
		Game1.playSound("cancel", (int?)null);
		Game1.drawObjectDialogue(message);
	}

	private void AssignOwnership(Chest chest)
	{
		lock (ModDataLock)
		{
			if (ParseOwner(chest) == -1)
			{
				((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)chest).modData)["LockedChests.Owner"] = Game1.player.UniqueMultiplayerID.ToString();
			}
		}
	}

	private void ShowLockManagementDialog(Chest chest)
	{
		object obj;
		if (ParseLock(chest) != Lock.Locked)
		{
			ITranslationHelper? iTranslations = ITranslations;
			Translation val = ((iTranslations != null) ? iTranslations.Get("Unlocked") : null);
			obj = ((val != null) ? Translation.op_Implicit(val) : "Unlocked");
		}
		else
		{
			ITranslationHelper? iTranslations2 = ITranslations;
			Translation val = ((iTranslations2 != null) ? iTranslations2.Get("Locked") : null);
			obj = ((val != null) ? Translation.op_Implicit(val) : "Locked");
		}
		string statusText = (string)obj;
		string infoLine = "Your Chest is " + statusText;
		if (ParseLock(chest) == Lock.Open)
		{
			ShowLockConfirmation(chest, infoLine);
		}
		else
		{
			ShowUnlockConfirmation(chest, infoLine);
		}
	}

	private void ShowLockConfirmation(Chest chest, string infoLine)
	{
		ITranslationHelper? iTranslations = ITranslations;
		Translation val = ((iTranslations != null) ? iTranslations.Get("Lock_Chest_Question") : null);
		string question = ((val != null) ? Translation.op_Implicit(val) : "Do you want to lock this chest?").ToString();
		string full = infoLine + ". " + question;
		Response[] responses = (Response[])(object)new Response[2]
		{
			new Response("Yes", "Yes"),
			new Response("No", "No")
		};
		Game1.currentLocation.createQuestionDialogue(full, responses, (afterQuestionBehavior)delegate(Farmer farmer, string answer)
		{
			if (answer == "Yes")
			{
				SetChestLocked(chest, locked: true);
				Game1.playSound("hammer", (int?)null);
				ITranslationHelper? iTranslations2 = ITranslations;
				Translation val2 = ((iTranslations2 != null) ? iTranslations2.Get("Chest_Locked_Message") : null);
				Game1.drawObjectDialogue((val2 != null) ? Translation.op_Implicit(val2) : "Chest locked.");
			}
			else
			{
				Game1.playSound("cancel", (int?)null);
			}
		}, (NPC)null);
	}

	private void ShowUnlockConfirmation(Chest chest, string infoLine)
	{
		ITranslationHelper? iTranslations = ITranslations;
		Translation val = ((iTranslations != null) ? iTranslations.Get("Unlock_Chest_Question") : null);
		string question = ((val != null) ? Translation.op_Implicit(val) : "Do you want to unlock this chest?").ToString();
		string full = infoLine + ". " + question;
		Response[] responses = (Response[])(object)new Response[2]
		{
			new Response("Yes", "Yes"),
			new Response("No", "No")
		};
		Game1.currentLocation.createQuestionDialogue(full, responses, (afterQuestionBehavior)delegate(Farmer farmer, string answer)
		{
			if (answer == "Yes")
			{
				SetChestLocked(chest, locked: false);
				Game1.playSound("coin", (int?)null);
				ITranslationHelper? iTranslations2 = ITranslations;
				Translation val2 = ((iTranslations2 != null) ? iTranslations2.Get("Chest_Unlocked_Message") : null);
				Game1.drawObjectDialogue((val2 != null) ? Translation.op_Implicit(val2) : "Chest unlocked.");
			}
			else
			{
				Game1.playSound("cancel", (int?)null);
			}
		}, (NPC)null);
	}

	private void ApplyPatchesSafely()
	{
		try
		{
			Patches.Patch(((Mod)this).Monitor);
			MoveGuards.Patch(((Mod)this).Monitor);
			TextureManager.Initialize();
			AllChestsMenuCompat.Patch(((Mod)this).Monitor);
		}
		catch (Exception value)
		{
			((Mod)this).Monitor.Log($"Initialization error: {value}", (LogLevel)4);
		}
	}

	public static long ParseOwner(Chest chest)
	{
		if (((chest != null) ? ((Item)chest).modData : null) == null)
		{
			return -1L;
		}
		lock (ModDataLock)
		{
			string s = default(string);
			long id;
			return (((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)chest).modData).TryGetValue("LockedChests.Owner", ref s) && long.TryParse(s, out id)) ? id : (-1);
		}
	}

	public static Lock ParseLock(Chest chest)
	{
		if (((chest != null) ? ((Item)chest).modData : null) == null)
		{
			return Lock.Open;
		}
		lock (ModDataLock)
		{
			string s = default(string);
			Lock st;
			return (((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)chest).modData).TryGetValue("LockedChests.Lock", ref s) && Enum.TryParse<Lock>(s, out st)) ? st : Lock.Open;
		}
	}

	public static Farmer? TryGetPlayer(long uniqueId)
	{
		try
		{
			return Game1.GetPlayer(uniqueId, false) ?? Game1.MasterPlayer;
		}
		catch (Exception value)
		{
			IMonitor? iMonitor = IMonitor;
			if (iMonitor != null)
			{
				iMonitor.Log($"GetPlayer error {uniqueId}: {value}", (LogLevel)3);
			}
			return Game1.MasterPlayer;
		}
	}

	public static bool IsChestAccessibleToPlayer(Chest chest, Farmer player)
	{
		if (chest == null || ((NetFieldBase<bool, NetBool>)(object)chest.playerChest)?.Value != true || player == null)
		{
			return false;
		}
		Lock st = ParseLock(chest);
		if (st != Lock.Locked)
		{
			return true;
		}
		long owner = ParseOwner(chest);
		if (owner != -1)
		{
			return owner == player.UniqueMultiplayerID;
		}
		return true;
	}

	public static void SetChestLocked(Chest chest, bool locked, long? ownerId = null)
	{
		if (((chest != null) ? ((Item)chest).modData : null) == null)
		{
			return;
		}
		lock (ModDataLock)
		{
			((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)chest).modData)["LockedChests.Lock"] = (locked ? Lock.Locked.ToString() : Lock.Open.ToString());
			if (ownerId.HasValue)
			{
				((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)chest).modData)["LockedChests.Owner"] = ownerId.Value.ToString();
			}
			else if (locked && ParseOwner(chest) == -1)
			{
				((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)chest).modData)["LockedChests.Owner"] = Game1.player.UniqueMultiplayerID.ToString();
			}
		}
	}
}
