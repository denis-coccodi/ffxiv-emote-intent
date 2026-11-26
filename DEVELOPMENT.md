# Developing Emote Intent

For building, testing and changing the plugin. Players only need the [README](README.md).

## Phrase bank

The phrase bank covers 292 of the game's 295 emotes. Near-identical emotes share their phrases and play whichever the player has unlocked (Jump for Joy 1–5, the penlight cheers, the Ranger poses, both winks). Only Visor and Change Pose (utilities) and Water Flip (underwater only) play by name or command alone. The optional AI help (`AiFallback`, off by default) asks a model in Ollama on the player's PC what text the phrase bank isn't sure of means, and turns its plain words into emotes through the phrase bank (`IntentResolver.ResolveMeanings`).

`EmoteIntent.Core/Data/Phrases/en/<command>.txt` holds, for each emote, the ways players might say it (one phrase per line; `#` lines are comments). A line `@ /a /b /c` makes a file a family of near-identical emotes, or a preference list for a feeling (`feelexcited.txt`: Jump for Joy, else Psych, else Delighted; `feelflying.txt`: Advent of Light, else Simulation, Blown Away, Backflip): its phrases play the first one you have unlocked. Strong feelings prefer a full-body emote over a face-only expression, even when the word is also an expression's name ("sad" plays Slump, the Sad frown only without it). `/feel` ranks emotes by the words your text shares with their phrases, rarer words counting more, and word order a little. Lead-in words that say nothing about the emote ("feel like being spooky", "i'm feeling", "i want to be") are also tried without, and filler words ("like", "my", "it") count for little: they never make a match on their own.

A few words rule emotes out where sharing words can't tell meanings apart (`EmoteIntent.Core/Data/rule-outs.txt`): a cold word ("cold", "freezing", "chills") never chooses Tremble or the other fear emotes, unless a fear word is there too ("cold and scared"). Naming an emote still plays it.

Typos are fixed before matching, like a phone keyboard's autocorrect and without AI: a word the phrase bank doesn't know becomes the closest word it does ("exicted" → "excited", "hapy" → "happy", "spooookyy" → "spooky"), and the chat says so (`Read "exicted" as "excited".`) so a wrong guess shows at once. Words the bank knows never change, nor do real English words it doesn't know ("raining" isn't read as "training"; the public-domain ENABLE word list tells them apart), words under four letters are left alone, and a word with no close match stays as typed. An emote named exactly as typed is never corrected.

The phrases follow what each emote does in the game: its own description, exported with the emote list to `EmoteIntent.Core/Data/emotes.json` ("Cup your face in barely contained ecstasy."), and screenshots where that isn't enough. They were written with AI help (Claude) and are checked by tests: every file names real emotes, has at least 30 phrases (20 for a family), no phrase belongs to two files, and about 150 everyday words and sentences ("excited", "jealous", "brb", "i'm back") must keep playing the emote they play now.

## Try a development build

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

By default each model is tried two ways (`--mode pick` or `meaning` for one): choosing among every emote's guide line, and saying what the text means in plain words that the phrase bank turns into emotes, also combined as the plugin does it (phrase bank first, model only when the bank isn't sure; `IntentResolver.ResolveMeanings`). It prints how often each one's first choice is right, how often a right emote is among its first three, how fast the model answers, and every miss. Choosing among every emote did worse than the phrase bank for every model tried; the combination with gemma3:4b did best (78/97 first choices against the bank's 75, and 7 more of 37 disputed sentences judged blind by a player), so the plugin's optional AI uses it.

The answer key in `cases.txt` is one person's opinion. `--sheet file` also writes a blind sheet: every sentence where the setups' first choices differ, with those emotes shuffled and unlabelled, for someone to tick the ones that fit. `dotnet run --project tools/AiEval -c Release -- --judge file` then scores each setup against those ticks, without running the models again (their answers are saved beside the sheet).

Warnings are build errors, with .NET's recommended analyzers and a list of banned APIs (`BannedSymbols.txt`). The first build sets up two git hooks: Conventional Commits messages, and the three checks above before every push.

Changes go through pull requests to `main`; see [CLAUDE.md](CLAUDE.md). A tag `vX.Y.Z.W` on `main` releases that version (`.github/workflows/release.yaml`, `tools/New-RepoJson.ps1`).
