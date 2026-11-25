# Emote Intent

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV. Type `/feel` and the emote you want, and it plays it if your character has it unlocked.

**Early version.** `/feel` understands what you mean for 248 of the game's 295 emotes (`/feel so happy to see you`, `/feel give me a big hug`, `/feel idk`, `/feel i'm cold`, `/feel o7`). The rest, mostly colour and side variants such as the penlight cheers and Ranger poses, and a few dances, play by name or command (`/feel cheer wave pink`); more emotes come with each update. Any emote also plays by its name, command or alias (`/feel hug`, `/feel /hug`). Matching runs on your PC from a phrase bank shipped with the plugin; AI will only ever be an option you switch on with your own key.

## Commands

| Command | What it does |
| --- | --- |
| `/feel` | Says how many emotes you have unlocked. |
| `/feel <what you mean>` | Plays the unlocked emote that clearly fits, the same as typing its command. One command plays one emote. |
| | When a few emotes might fit, it lists them in chat as links and plays nothing: click the one you want (or type it). |
| `/feel <what you mean> motion` | The same, without the emote's line in the chat log (the game's `/hug motion`). Suggested links play that way too. |
| `/feel <what you mean> text` | The same, with the chat line: the game's default today. |

`motion` and `text` only count as the last word, and not when the whole text is a phrase that ends with them ("reading the quest text").

## Phrase bank

`EmoteIntent.Core/Data/Phrases/en/<command>.txt` holds, for each emote, the ways players might say it (one phrase per line; `#` lines are comments). `/feel` ranks emotes by the words your text shares with their phrases, rarer words counting more, and word order a little. The phrases were written with AI help (Claude) and are checked by tests: every file is a real emote, has at least 30 phrases, and no phrase belongs to two emotes.

## Install

The plugin isn't released yet. To try a build:

1. Build it (below): every local build, from any branch or worktree, also copies the plugin to `%LOCALAPPDATA%\EmoteIntent\DevPlugin`. Or download the `EmoteIntent` artifact from a [test run](https://github.com/denis-coccodi/ffxiv-emote-intent/actions/workflows/test.yaml) and unzip the `latest.zip` inside it.
2. In the game, open `/xlsettings` → Experimental → Dev Plugin Locations, add the path to the DLL itself (a folder is refused): `%LOCALAPPDATA%\EmoteIntent\DevPlugin\EmoteIntent.dll` written out in full (e.g. `C:\Users\<you>\AppData\Local\EmoteIntent\DevPlugin\EmoteIntent.dll`), or `EmoteIntent.dll` from the unzipped `latest.zip`. Press **+**, then save.
3. Enable it in `/xlplugins` → Dev Tools → Installed Dev Plugins, and turn on its automatic reloading: each new build then loads by itself.

## Development

Needs the .NET 10 SDK and XIVLauncher with Dalamud installed (the build uses its dev files in `%AppData%\XIVLauncher\addon\Hooks\dev`).

```
dotnet build -c Release
dotnet test -c Release --no-build
dotnet format --verify-no-changes
```

- `EmoteIntent.Core`: everything that doesn't need the game (the emote model, matching). The tests cover it.
- `EmoteIntent`: the Dalamud plugin (the `/feel` command, reading emotes from the game data, playing them).
- `tools/EmoteExport`: writes every emote that can be played by command to `EmoteIntent.Core/Data/emotes.json`.

### Emote data

`EmoteIntent.Core/Data/emotes.json` lists the game's emotes (295 today) with their commands and Emote list tab: the list the phrase bank is written against. After a patch that adds emotes, regenerate it from the installed game (English data; it finds the game through XIVLauncher's settings, or takes the game folder as an argument):

```
dotnet run --project tools/EmoteExport -c Release
```

Warnings are build errors, with .NET's recommended analyzers and a list of banned APIs (`BannedSymbols.txt`). The first build sets up two git hooks: Conventional Commits messages, and the three checks above before every push.

Changes go through pull requests to `main`; see [CLAUDE.md](CLAUDE.md).

## License

[AGPL-3.0](LICENSE), like Dalamud itself.
