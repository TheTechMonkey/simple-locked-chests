# Simple Locked Chests

A Stardew Valley SMAPI mod for multiplayer farms. Lock your chests, storage furniture and house or shed door so other players can't get in.

## What it does

- **Chests and storage furniture** (dressers and the like): the first player to open one becomes its owner. The owner presses the lock key (F9 by default) while facing it to lock or unlock it. Nobody else can open, move, break or pick up a locked one. Junimo chests and mini shipping bins can't be locked.
- **House doors:** the farmhouse belongs to the host and each cabin to the farmhand who lives in it. Sheds and big sheds belong to whoever locks them first. Other players can't walk in through a locked front door, and nobody is ever locked inside. Gates, animal doors, barns, coops and town buildings can't be locked.
- **Chests Anywhere and All Chests Menu:** chests locked by someone else don't appear in them at all. Open chests appear as normal.
- **Host override** (off by default): lets the host open and unlock everything, so locks left by players who quit can be cleared.
- **Lock icons and hover text** show what's locked and who owns it. Everything is configurable through Generic Mod Config Menu.

Nexus Mods: https://www.nexusmods.com/stardewvalley/mods/37358

## Building

Requires the .NET 6 SDK or newer and an installed copy of Stardew Valley with SMAPI. The build uses [Pathoschild.Stardew.ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig), which finds the game and deploys the mod to your `Mods` folder.

```
dotnet build
```

## Source history

The original source was lost. This repository was rebuilt from the released 2.0.1 DLL and its debug symbols with ILSpy, so names and file layout match the original.
