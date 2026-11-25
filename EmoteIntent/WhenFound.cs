namespace EmoteIntent;

/// <summary>What /feel does once it has found the emote that fits best (a setting).</summary>
public enum WhenFound
{
    /// <summary>Play it, then list the others that might fit as links ("Also fits: …").</summary>
    PlayBest,

    /// <summary>Play it if it's the only one that fits, or was named; otherwise list them all as links and wait.</summary>
    AskWhenSeveral,

    /// <summary>Never play by itself: always list the matches as links, even a single one.</summary>
    AlwaysPick,
}
