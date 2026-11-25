using EmoteIntent.Core;
using Lumina.Excel.Sheets;

namespace EmoteIntent;

/// <summary>
/// Turns the game's Emote rows into Core's <see cref="EmoteInfo"/>. Compiled into both the plugin and
/// tools/EmoteExport, so the plugin and the exported emotes.json always agree on what an emote is.
/// </summary>
internal static class EmoteRows
{
    /// <summary>
    /// The emotes a player can play by typing a command, in the game's order. Rows with a name but no command
    /// (Stand Up, Visor, Snowball, Sleep, Change Pose…) are played by the game itself and are left out.
    /// </summary>
    public static IEnumerable<Emote> Playable(IEnumerable<Emote> rows) =>
        rows.Where(row => !row.Name.IsEmpty && row.TextCommand.IsValid && !row.TextCommand.Value.Command.IsEmpty)
            .OrderBy(row => row.Order)
            .ThenBy(row => row.RowId);

    /// <summary>The emote's name, command, other commands and Emote list tab, as plain text.</summary>
    public static EmoteInfo ToInfo(Emote row)
    {
        var command = row.TextCommand.Value;
        var aliases = new[] { command.ShortCommand, command.Alias, command.ShortAlias }
            .Select(text => text.ExtractText())
            .Where(text => text.Length > 0)
            .Distinct()
            .ToList();
        var category = row.EmoteCategory.ValueNullable?.Name.ExtractText() ?? string.Empty;
        return new EmoteInfo(row.RowId, row.Name.ExtractText(), command.Command.ExtractText(), aliases, category);
    }
}
