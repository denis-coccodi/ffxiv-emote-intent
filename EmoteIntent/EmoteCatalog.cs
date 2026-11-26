using Dalamud.Plugin.Services;
using EmoteIntent.Core;
using Lumina.Excel.Sheets;

namespace EmoteIntent;

/// <summary>Reads the emotes from the game data, and which of them the current character has unlocked.</summary>
internal sealed class EmoteCatalog(IDataManager dataManager, IUnlockState unlockState)
{
    private IReadOnlyList<EmoteInfo>? all;

    /// <summary>Every emote of the game that can be played by command, unlocked or not. Read once: game data doesn't change.</summary>
    public IReadOnlyList<EmoteInfo> All() =>
        all ??= EmoteRows.Playable(dataManager.GetExcelSheet<Emote>()).Select(EmoteRows.ToInfo).ToList();

    /// <summary>Every emote the character has unlocked and can play by command, in the game's own order.</summary>
    public IReadOnlyList<EmoteInfo> Unlocked() =>
        EmoteRows.Playable(dataManager.GetExcelSheet<Emote>())
            .Where(unlockState.IsEmoteUnlocked)
            .Select(EmoteRows.ToInfo)
            .ToList();
}
