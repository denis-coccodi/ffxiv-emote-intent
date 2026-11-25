using Dalamud.Plugin.Services;
using EmoteIntent.Core;
using Lumina.Excel.Sheets;

namespace EmoteIntent;

/// <summary>Reads the emotes from the game data, and which of them the current character has unlocked.</summary>
internal sealed class EmoteCatalog(IDataManager dataManager, IUnlockState unlockState)
{
    /// <summary>Every emote the character has unlocked and can play by command, in the game's own order.</summary>
    public IReadOnlyList<EmoteInfo> Unlocked() =>
        EmoteRows.Playable(dataManager.GetExcelSheet<Emote>())
            .Where(unlockState.IsEmoteUnlocked)
            .Select(EmoteRows.ToInfo)
            .ToList();
}
