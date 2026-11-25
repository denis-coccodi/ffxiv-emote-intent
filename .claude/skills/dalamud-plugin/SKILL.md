---
name: dalamud-plugin
description: How the Emote Intent plugin works with Dalamud and the game - services, the plugin lifecycle and Dispose, the main thread, reading game data with Lumina, calling game code through FFXIVClientStructs, configuration and windows, Dalamud's rules (no automation, no emote loops, privacy), finding the right API in the installed version, and trying a build in the game. Use when working in the EmoteIntent project, adding a command, window, setting or game call, when Dalamud's API level changes, or when something works in tests but not in the game.
---

# Building on Dalamud

The plugin targets **Dalamud API 15** through `Dalamud.NET.Sdk/15.0.0` (`EmoteIntent/EmoteIntent.csproj`), .NET 10. Dalamud raises the API level with most game patches: plugins built for an older level stop loading until they're rebuilt with the new SDK.

## Find the API in the installed version, not from memory

Dalamud, Lumina and FFXIVClientStructs change names between versions, and AI models get them wrong (Dalamud's AI policy warns about exactly this). Before using a member, look it up in what the build compiles against: `%AppData%\XIVLauncher\addon\Hooks\dev`:

```
grep -o 'name="M:[^"]*IsEmoteUnlocked[^"]*"' "$APPDATA/XIVLauncher/addon/Hooks/dev/Dalamud.xml"
grep -a -o 'ProcessChatBoxEntry' "$APPDATA/XIVLauncher/addon/Hooks/dev/FFXIVClientStructs.dll"
```

`Dalamud.xml` and `FFXIVClientStructs.xml` document most members; for undocumented ones, search the DLL's strings, then let the compiler confirm the signature. Docs: dalamud.dev (the API reference and plugin-development pages).

Order of preference (dalamud.dev): a **Dalamud service** (e.g. `IUnlockState`), then **FFXIVClientStructs** (e.g. `UIModule.ProcessChatBoxEntry`), and only as a last resort a raw signature (`[Signature]`), which breaks on every patch.

## Services and lifecycle

- `Plugin` (implements `IDalamudPlugin`) is the only entry point. Services come in through `[PluginService]` static properties (`internal static IChatGui ChatGui { get; private set; } = null!;`); pass them on to classes through their constructors rather than reaching for the statics everywhere.
- **`Dispose` undoes everything the constructor did**, in reverse: remove command handlers, unsubscribe events (`UiBuilder.Draw`, `Framework.Update`), dispose windows, cancel background work. Players reload plugins without restarting the game; anything left behind keeps running or crashes.
- Commands: one `CommandManager.AddHandler` per command with a `HelpMessage`; the handler parses its arguments and hands off to a class.
- Log with `IPluginLog` (`Log.Debug` for detail, `Log.Warning`/`Log.Error` with the exception), never `Console` (banned). Tell the player things with `IChatGui.Print`, short and in plain words.

## Threads

Command handlers, `Framework.Update` and UI drawing run on the game's main thread, and only there may game memory and most services be touched. Slow work goes off-thread (`Task.Run`), and its result comes back with `IFramework.RunOnFrameworkThread`. Never block the main thread (the blocking APIs are banned at build time).

## Game data (Lumina)

- Sheets come from `IDataManager.GetExcelSheet<T>()` with types from `Lumina.Excel.Sheets` (`Emote`, `TextCommand`, `EmoteCategory`).
- References are `RowRef<T>`: check `.IsValid` (or use `.ValueNullable`) before `.Value`.
- Text fields are `ReadOnlySeString`: `.ExtractText()` for plain text, `.IsEmpty` to skip blank rows.
- Read game data once per need and turn it into Core's types (`EmoteInfo`) at the edge; Core never sees Lumina types.
- The sheets follow the player's game language. Matching data (the phrase bank) must say which language it covers.

## Calling game code (FFXIVClientStructs)

- `unsafe` is allowed in the plugin project only, inside small, documented wrappers (`ChatCommand`). Free what you allocate (`Utf8String.FromString` → `Dtor(true)` in a `finally`).
- Validate inputs before they reach the game: `ChatCommand.Run` accepts one command, starting with `/`, with no newlines.

## Dalamud's rules (dalamud.dev/plugin-publishing/restrictions)

These hold even though the plugin ships from our own repository:

- **No automation**: one player action → at most one game action. Never loop or repeat emotes, never queue several, never act on a timer or on other players' chat.
- Nothing may reach the game servers that the player couldn't send by typing; emotes go through the chat box exactly as typed.
- No combat or PvP advantage, no data about other players collected or sent anywhere.
- Network calls only when the player opted in (the AI fallback, with their own key or local model), over HTTPS, sending only the phrase and the unlocked emote names. Never ship a key.

## Configuration and windows

- Settings: a class implementing `IPluginConfiguration` with a `Version`, loaded with `PluginInterface.GetPluginConfig()` and saved with `PluginInterface.SavePluginConfig()`. Bump `Version` and migrate when a setting's meaning changes.
- Windows: Dalamud's windowing API (`WindowSystem` + `Window` subclasses), drawn from `UiBuilder.Draw`; open the settings window from `UiBuilder.OpenConfigUi` and the main one from `UiBuilder.OpenMainUi`.

## Trying it in the game

1. `dotnet build -c Release` in whichever checkout or worktree holds the change. Besides `EmoteIntent\bin\Release\`, every local build copies the plugin (`EmoteIntent.dll`, `EmoteIntent.Core.dll`, `EmoteIntent.json`) to the fixed folder `%LOCALAPPDATA%\EmoteIntent\DevPlugin` (`CopyToDevPluginDir` in `EmoteIntent.csproj`; not in CI). The last build wins, so build the branch the user should try last, and tell them which one it is.
2. The user's Dalamud points at `%LOCALAPPDATA%\EmoteIntent\DevPlugin\EmoteIntent.dll` once, with automatic reloading on: never ask them to change the path for a branch or worktree. First time only: `/xlsettings` → Experimental → Dev Plugin Locations → add that full path (Dalamud refuses a folder) → **+** → Save; then `/xlplugins` → Dev Tools → Installed Dev Plugins → enable Emote Intent and its automatic reloading.
3. Removing a worktree doesn't break their setup; rebuild `main` afterwards if the fixed folder should hold what `main` has.
4. `/xllog` shows the plugin's log; `/xldata` browses the game's sheets.

Say in the pull request whether a change was tried in the game and what was checked; tests can't cover game calls.

## Manifest

The plugin's name, description, tags and version are set in `EmoteIntent.csproj` (DalamudPackager writes `EmoteIntent.json` from them). `AssemblyName`/`InternalName` `EmoteIntent` must never change once released. Keep the description true to what the plugin does now.
