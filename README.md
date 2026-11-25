# Emote Intent

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV. Type `/feel` and the emote you want, and it plays it if your character has it unlocked.

**Early version.** `/feel` understands what you mean for 292 of the game's 295 emotes (`/feel so happy to see you`, `/feel excited`, `/feel give me a big hug`, `/feel idk`, `/feel i'm cold`, `/feel o7`). Near-identical emotes share their phrases and play whichever you have unlocked (Jump for Joy 1–5, the penlight cheers, the Ranger poses, both winks). Only Visor and Change Pose (utilities) and Water Flip (underwater only) play by name or command alone. Any emote also plays by its name, command or alias (`/feel hug`, `/feel /hug`). Matching runs on your PC from a phrase bank shipped with the plugin; AI will only ever be an option you switch on with your own key.

## Commands

| Command | What it does |
| --- | --- |
| `/feel` | Says how many emotes you have unlocked. |
| `/feel <what you mean>` | Plays the unlocked emote that clearly fits, the same as typing its command. One command plays one emote. |
| | When a few emotes might fit, it lists them in chat as links and plays nothing: click the one you want (or type it). |
| | After it plays one, it lists other emotes that might fit too, as links ("Also fits: /carrybook…"). |
| `/feel <what you mean> motion` | The same, without the emote's line in the chat log (the game's `/hug motion`). Suggested links play that way too. |
| `/feel <what you mean> text` | The same, with the emote's line (the plain command). |

`motion` and `text` only count as the last word, and not when the whole text is a phrase that ends with them ("reading the quest text").

## Settings

`/feelconfig` (or the gear button in `/xlplugins`) opens the settings:

- **When /feel finds an emote**: **Play it** (default), or **Let me pick**: nothing plays by itself; the matches are listed as links, even a single one ("Pick one: /read (Read), /carrybook (Carry Book)…"), and you click the one you want.
- **Also list other emotes that might fit** (on by default): after an emote plays, "Also fits: …" lists the others as links; with "Let me pick", they join the list.
- **Say which emote played** (off by default; only with "Play it"): "Played /cheer (Cheer)." after /feel played an emote by itself, since the game's own line ("You cheer enthusiastically.") doesn't name the command. Never after you click a link.

Whether the game writes an emote's own line in the chat log is a game setting; `motion` at the end of a `/feel` skips it for that one emote.

## Phrase bank

`EmoteIntent.Core/Data/Phrases/en/<command>.txt` holds, for each emote, the ways players might say it (one phrase per line; `#` lines are comments). A line `@ /a /b /c` makes a file a family of near-identical emotes, or a preference list for a feeling (`feelexcited.txt`: Jump for Joy, else Psych, else Delighted): its phrases play the first one you have unlocked. Strong feelings prefer a full-body emote over a face-only expression, even when the word is also an expression's name ("sad" plays Slump, the Sad frown only without it). `/feel` ranks emotes by the words your text shares with their phrases, rarer words counting more, and word order a little. Lead-in words that say nothing about the emote ("feel like being spooky", "i'm feeling", "i want to be") are also tried without, and filler words ("like", "my", "it") count for little: they never make a match on their own.

The phrases follow what each emote does in the game: its own description, exported with the emote list to `EmoteIntent.Core/Data/emotes.json` ("Cup your face in barely contained ecstasy."), and screenshots where that isn't enough. They were written with AI help (Claude) and are checked by tests: every file names real emotes, has at least 30 phrases (20 for a family), no phrase belongs to two files, and about 150 everyday words and sentences ("excited", "jealous", "brb", "i'm back") must keep playing the emote they play now.

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
