# Emote Intent

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV. Type `/feel` and what you mean, and it plays the emote that fits, from the ones your character has unlocked: `/feel so happy to see you`, `/feel excited`, `/feel give me a big hug`, `/feel idk`, `/feel o7`. Any emote also plays by its name (`/feel hug`).

It works entirely on its own, on your PC: no AI, account or download needed.

## Install

Emote Intent isn't on Dalamud's official plugin list: you add its plugin repository once, then install it like any other plugin.

1. In the game, type `/xlsettings` and open the **Experimental** tab.
2. Under **Custom Plugin Repositories**, Dalamud first shows a **READ THIS FIRST!** warning: custom repositories aren't checked by the Dalamud team, so only add ones from people you trust. After a few seconds, press **Ok, I have read and understood this warning**.
3. Paste this URL into the empty field, press **+**, and make sure **Enabled** is ticked next to it:
   ```
   https://github.com/denis-coccodi/ffxiv-emote-intent/releases/latest/download/repo.json
   ```
   Then press the **Save** button.
4. Type `/xlplugins`, open **All Plugins**, search for **Emote Intent** and press **Install**.
5. Type `/feel` to see how to use it, or just try `/feel so happy to see you`.

Updates arrive like any plugin's: Dalamud offers them in `/xlplugins` (reopen it to check for new ones). Each [release](https://github.com/denis-coccodi/ffxiv-emote-intent/releases) lists what changed. To remove it, uninstall it in `/xlplugins` and delete the URL from the repository list.

## Commands

| Command | What it does |
| --- | --- |
| `/feel` or `/feel ?` | Opens the Emote Intent window on its Help tab: how to use it, and how many emotes you have unlocked. The plugin list's Open button opens it too. |
| `/feel <what you mean>` | Plays the unlocked emote that fits, the same as typing its command. One command plays one emote. Splash and Water Float are only chosen while you swim, Water Flip while you dive, since the game refuses them elsewhere (naming one still tries it). |
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
- **Optional extra: AI help** (off by default, folded away): you don't need this, and most players never will. For players who already use AI programs: when /feel isn't sure, it can ask a free AI model running on your own PC, in [Ollama](https://ollama.com); what you type never leaves your PC. To use it, install Ollama, run `ollama pull gemma3:4b` once (about 3 GB), tick the box, and press **Check** to see it's ready. If Ollama isn't running, /feel says so and works as usual.

Whether the game writes an emote's own line in the chat log is a game setting; `motion` at the end of a `/feel` skips it for that one emote.

## How it understands you

/feel compares what you type with thousands of ways players say each emote, written for this plugin and checked against what every emote does in the game. Strong feelings prefer a full-body emote over a small facial expression ("sad" plays Slump). Typos are fixed, and the chat says how it read them (`Read "exicted" as "excited".`), so a wrong guess shows at once. It all runs on your PC.

Something plays the wrong emote, or nothing? [Open an issue](https://github.com/denis-coccodi/ffxiv-emote-intent/issues) with what you typed and the emote you meant.

Building or changing the plugin: see [DEVELOPMENT.md](DEVELOPMENT.md).

## License

[AGPL-3.0](LICENSE), like Dalamud itself.
