# Simple Locked Chests

A Stardew Valley SMAPI mod with simple chest locks: Open or Locked. The first player to open a chest becomes its owner, and only the owner can open, move or break a locked chest. Supports Generic Mod Config Menu, Chests Anywhere and All Chests Menu.

Nexus Mods: https://www.nexusmods.com/stardewvalley/mods/37358

## Building

Requires the .NET 6 SDK or newer and an installed copy of Stardew Valley with SMAPI. The build uses [Pathoschild.Stardew.ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig), which finds the game and deploys the mod to your `Mods` folder.

```
dotnet build
```

## Source history

The original source was lost. This repository was rebuilt from the released 2.0.1 DLL and its debug symbols with ILSpy, so names and file layout match the original.
