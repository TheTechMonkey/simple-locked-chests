using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LockChests.Framework;

/// <summary>Handles the lock key: shows the lock or unlock prompt for whatever the player is facing.</summary>
internal sealed class LockKeyHandler
{
	private readonly Func<ModConfig> getConfig;
	private readonly IMonitor monitor;

	public LockKeyHandler(Func<ModConfig> getConfig, IMonitor monitor)
	{
		this.getConfig = getConfig;
		this.monitor = monitor;
	}

	public void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
	{
		if (!Context.IsWorldReady || !Context.CanPlayerMove || e.Button != this.getConfig().OpenKey)
			return;

		try
		{
			Farmer player = Game1.player;
			Lockable? lockable = Lockables.AtTile(Game1.currentLocation, player.GetGrabTile());
			if (lockable is null)
			{
				Game1.playSound("cancel");
				return;
			}

			if (LockRules.IsLockedAgainst(lockable, player))
			{
				LockMessages.ShowLockedOut(lockable);
				return;
			}

			if (!LockRules.CanManage(lockable, player))
			{
				LockMessages.ShowNotOwner(lockable);
				return;
			}

			lockable.ClaimIfUnowned(player);
			this.AskToToggle(lockable);
		}
		catch (Exception ex)
		{
			this.monitor.Log($"Failed handling the lock key: {ex}", LogLevel.Error);
			Game1.playSound("cancel");
		}
	}

	/// <summary>Ask whether to lock or unlock, then apply the answer.</summary>
	private void AskToToggle(Lockable lockable)
	{
		bool isLocked = lockable.State == LockState.Locked;
		string question = isLocked
			? LockMessages.Format("Unlock_Question", "This {{Thing}} is locked. Unlock it?", lockable)
			: LockMessages.Format("Lock_Question", "This {{Thing}} is open. Lock it?", lockable);

		Response[] responses =
		{
			new("Yes", I18n.Get("Yes", "Yes")),
			new("No", I18n.Get("No", "No"))
		};

		Game1.currentLocation.createQuestionDialogue(question, responses, (_, answer) =>
		{
			if (answer != "Yes")
			{
				Game1.playSound("cancel");
				return;
			}

			LockState newState = isLocked ? LockState.Open : LockState.Locked;
			lockable.SetState(newState);
			Game1.playSound(newState == LockState.Locked ? "hammer" : "coin");
			Game1.drawObjectDialogue(newState == LockState.Locked
				? LockMessages.Format("Locked_Message", "The {{Thing}} is now locked.", lockable)
				: LockMessages.Format("Unlocked_Message", "The {{Thing}} is now unlocked.", lockable));
		});
	}
}
