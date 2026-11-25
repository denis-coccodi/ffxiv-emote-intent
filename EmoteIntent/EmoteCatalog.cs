using Dalamud.Plugin.Services;
using EmoteIntent.Core;
using Lumina.Excel.Sheets;

namespace EmoteIntent;

/// <summary>Reads the emotes from the game data, and which of them the current character has unlocked.</summary>
internal sealed class EmoteCatalog(IDataManager dataManager, IUnlockState unlockState)
{
    /// <summary>Every emote the character can play by command, in the game's own order.</summary>
    public IReadOnlyList<EmoteInfo> Unlocked() =>
        dataManager.GetExcelSheet<Emote>()
            .Where(row => row.TextCommand.IsValid && !row.Name.IsEmpty && unlockState.IsEmoteUnlocked(row))
            .OrderBy(row => row.Order)
            .Select(ToInfo)
            .ToList();

    private static EmoteInfo ToInfo(Emote row)
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
