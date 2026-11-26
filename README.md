# Emote Intent

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV. Type `/feel` and the emote you want, and it plays it if your character has it unlocked.

**Early version.** `/feel` understands what you mean for 292 of the game's 295 emotes (`/feel so happy to see you`, `/feel excited`, `/feel give me a big hug`, `/feel idk`, `/feel i'm cold`, `/feel o7`). Near-identical emotes share their phrases and play whichever you have unlocked (Jump for Joy 1–5, the penlight cheers, the Ranger poses, both winks). Only Visor and Change Pose (utilities) and Water Flip (underwater only) play by name or command alone. Any emote also plays by its name, command or alias (`/feel hug`, `/feel /hug`). Matching runs on your PC from a phrase bank shipped with the plugin; AI will only ever be an option you switch on with your own key.

## Commands

| Command | What it does |
| --- | --- |
| `/feel` or `/feel ?` | Opens the Emote Intent window on its Help tab: how to use it, and how many emotes you have unlocked. The plugin list's Open button opens it too. |
| `/feel <what you mean>` | Plays the unlocked emote that fits, the same as typing its command. One command plays one emote. |
| | When several might fit, it lists them in chat as links and plays nothing: click the one you want (or type it). See Settings for when it asks. |
| `/feel <emote you don't have>` | Plays nothing: says you don't have it unlocked and lists the closest emotes you do have, as links. |
| `/feel all <bundle>` | Lists every emote of a bundle you have unlocked, as links: `/feel all eat`, `/feel all jumps`, `/feel all dances`, `/feel all penlights`. Also "all the …" or "every …". |
| `/feel all` | Lists the bundles, as links: kinds of emote (eat, drink, jumps, dances, poses, penlights, paints, greetings, salutes, laughs, workouts, sits, books, spells, weapons, water, photos, gestures, work, silly) and feelings (friends, prayer, love, happy, cheer, angry, sad, scared, surprised, think, expressions). Every emote is in one, but Change Pose, Visor and Shiver. |
| `/feel <what you mean> motion` | The same, without the emote's line in the chat log (the game's `/hug motion`). Suggested links play that way too. |
| `/feel <what you mean> text` | The same, with the emote's line (the plain command). |

When a listed or played emote belongs to a bundle, the line ends with a link to the rest ("See all 9 eat emotes."). Bundles are in `EmoteIntent.Core/Data/bundles.txt`.

`motion` and `text` only count as the last word, and not when the whole text is a phrase that ends with them ("reading the quest text").

## Settings

`/feelconfig` (or the gear button in `/xlplugins`) opens the settings:

- **When /feel finds an emote**, one of:
  - **Play the best match**: it plays, then "Also fits: …" lists the others that might fit, as links.
  - **Ask when several fit** (default): it plays only when it's the only one that fits, or you typed its name or command (`/feel hug`). Otherwise nothing plays: all of them are listed as links, best first, and you click one.
  - **Always let me pick**: nothing plays by itself; the matches are listed as links, even a single one.
- **Say which emote played** (off by default): "Played /cheer (Cheer)." after /feel played an emote by itself, since the game's own line ("You cheer enthusiastically.") doesn't name the command. Never after you click a link.

Whether the game writes an emote's own line in the chat log is a game setting; `motion` at the end of a `/feel` skips it for that one emote.

## Phrase bank

`EmoteIntent.Core/Data/Phrases/en/<command>.txt` holds, for each emote, the ways players might say it (one phrase per line; `#` lines are comments). A line `@ /a /b /c` makes a file a family of near-identical emotes, or a preference list for a feeling (`feelexcited.txt`: Jump for Joy, else Psych, else Delighted; `feelflying.txt`: Advent of Light, else Simulation, Blown Away, Backflip): its phrases play the first one you have unlocked. Strong feelings prefer a full-body emote over a face-only expression, even when the word is also an expression's name ("sad" plays Slump, the Sad frown only without it). `/feel` ranks emotes by the words your text shares with their phrases, rarer words counting more, and word order a little. Lead-in words that say nothing about the emote ("feel like being spooky", "i'm feeling", "i want to be") are also tried without, and filler words ("like", "my", "it") count for little: they never make a match on their own.

Typos are fixed before matching, like a phone keyboard's autocorrect and without AI: a word the phrase bank doesn't know becomes the closest word it does ("exicted" → "excited", "hapy" → "happy", "spooookyy" → "spooky"), and the chat says so (`Read "exicted" as "excited".`) so a wrong guess shows at once. Words the bank knows never change, nor do real English words it doesn't know ("raining" isn't read as "training"; the public-domain ENABLE word list tells them apart), words under four letters are left alone, and a word with no close match stays as typed. An emote named exactly as typed is never corrected.

The phrases follow what each emote does in the game: its own description, exported with the emote list to `EmoteIntent.Core/Data/emotes.json` ("Cup your face in barely contained ecstasy."), and screenshots where that isn't enough. They were written with AI help (Claude) and are checked by tests: every file names real emotes, has at least 30 phrases (20 for a family), no phrase belongs to two files, and about 150 everyday words and sentences ("excited", "jealous", "brb", "i'm back") must keep playing the emote they play now.

## Install

Emote Intent comes from its own plugin repository, not Dalamud's official list:

1. In the game, open `/xlsettings` → Experimental → Custom Plugin Repositories, paste this URL, press **+**, then save:
   ```
   https://github.com/denis-coccodi/ffxiv-emote-intent/releases/latest/download/repo.json
   ```
2. Open `/xlplugins` → All Plugins, find **Emote Intent** and install it. Updates come the same way as for any plugin.
3. Type `/feel` to see how to use it.

Each [release](https://github.com/denis-coccodi/ffxiv-emote-intent/releases) lists what changed.

### Try a development build

1. Build it (below): every local build, from any branch or worktree, also copies the plugin to `%LOCALAPPDATA%\EmoteIntent\DevPlugin`. Or download the `EmoteIntent` artifact from a [test run](https://github.com/denis-coccodi/ffxiv-emote-intent/actions/workflows/test.yaml) and unzip the `latest.zip` inside it.
2. In the game, open `/xlsettings` → Experimental → Dev Plugin Locations, add the path to the DLL itself (a folder is refused): `%LOCALAPPDATA%\EmoteIntent\DevPlugin\EmoteIntent.dll` written out in full (e.g. `C:\Users\<you>\AppData\Local\EmoteIntent\DevPlugin\EmoteIntent.dll`), or `EmoteIntent.dll` from the unzipped `latest.zip`. Press **+**, then save. Don't install the released plugin at the same time.
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

`EmoteIntent.Core/Data/emote-guide.txt` describes each emote in more depth (what the character does, and when it fits), written for this project from the game's descriptions and in-game checks, not copied from wikis. It's there for an optional AI model choosing among emotes; a test checks every emote has a line.

### AI evaluation

`tools/AiEval` compares the phrase bank with a model running in [Ollama](https://ollama.com) on the same PC, on about 100 everyday sentences neither was tuned on (`tools/AiEval/cases.txt`, each with the emotes that count as right). With Ollama installed and a model pulled (`ollama pull qwen3:4b`):

```
dotnet run --project tools/AiEval -c Release -- qwen3:4b granite4:3b
```

It prints how often each one's first choice is right, how often a right emote is among its first three, how fast the model answers, and every miss. The plugin doesn't use a model yet.

Warnings are build errors, with .NET's recommended analyzers and a list of banned APIs (`BannedSymbols.txt`). The first build sets up two git hooks: Conventional Commits messages, and the three checks above before every push.

Changes go through pull requests to `main`; see [CLAUDE.md](CLAUDE.md). A tag `vX.Y.Z.W` on `main` releases that version (`.github/workflows/release.yaml`, `tools/New-RepoJson.ps1`).

## License

[AGPL-3.0](LICENSE), like Dalamud itself.
