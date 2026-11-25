namespace EmoteIntent.Core;

/// <summary>An emote as the game data describes it, independent of Dalamud.</summary>
/// <param name="Id">The row id in the game's Emote sheet.</param>
/// <param name="Name">The emote's name, as shown in the Emote list (e.g. "Hug").</param>
/// <param name="Command">The chat command that plays it, with its slash (e.g. "/hug").</param>
/// <param name="Aliases">Other commands for the same emote (short forms, aliases), with their slash.</param>
/// <param name="Category">The Emote list tab it sits in (e.g. "General", "Special", "Expressions").</param>
public sealed record EmoteInfo(uint Id, string Name, string Command, IReadOnlyList<string> Aliases, string Category);
