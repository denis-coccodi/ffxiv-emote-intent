# Emote Intent

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV. Type `/feel` and the emote you want, and it plays it if your character has it unlocked.

**Early version.** For now `/feel` matches an emote's name, command or alias (`/feel hug`, `/feel big clap`, `/feel bclap`). Matching what you mean (`/feel so happy to see you`) is being built. It will run on your PC from a phrase bank shipped with the plugin, and AI will only be an option you switch on with your own key.

## Commands

| Command | What it does |
| --- | --- |
| `/feel` | Says how many emotes you have unlocked. |
| `/feel <emote>` | Plays that emote, the same as typing its command. One command plays one emote. |

## Install

The plugin isn't released yet. To try a build:

1. Build it (below), or download the `EmoteIntent` artifact from a [test run](https://github.com/denis-coccodi/ffxiv-emote-intent/actions/workflows/test.yaml) and unzip the `latest.zip` inside it.
2. In the game, open `/xlsettings` → Experimental → Dev Plugin Locations, add the path to the DLL itself (a folder is refused): `EmoteIntent\bin\Release\EmoteIntent.dll` after a build, or `EmoteIntent.dll` from the unzipped `latest.zip`. Press **+**, then save.
3. Enable it in `/xlplugins` → Dev Tools → Installed Dev Plugins.

## Development

Needs the .NET 10 SDK and XIVLauncher with Dalamud installed (the build uses its dev files in `%AppData%\XIVLauncher\addon\Hooks\dev`).

```
dotnet build -c Release
dotnet test -c Release --no-build
dotnet format --verify-no-changes
```

- `EmoteIntent.Core`: everything that doesn't need the game (the emote model, matching). The tests cover it.
- `EmoteIntent`: the Dalamud plugin (the `/feel` command, reading emotes from the game data, playing them).

Warnings are build errors, with .NET's recommended analyzers and a list of banned APIs (`BannedSymbols.txt`). The first build sets up two git hooks: Conventional Commits messages, and the three checks above before every push.

Changes go through pull requests to `main`; see [CLAUDE.md](CLAUDE.md).

## License

[AGPL-3.0](LICENSE), like Dalamud itself.
