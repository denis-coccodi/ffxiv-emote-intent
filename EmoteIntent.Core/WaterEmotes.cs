namespace EmoteIntent.Core;

/// <summary>
/// Emotes the game only plays in water: Splash and Water Float while swimming or diving, Water Flip underwater.
/// Elsewhere the game refuses them, so they aren't chosen for what the player means.
/// </summary>
public static class WaterEmotes
{
    private static readonly HashSet<string> Swimming = new(["/splash", "/waterfloat"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Underwater = new(["/waterflip"], StringComparer.OrdinalIgnoreCase);

    /// <summary>The emotes that can play where the character is: without those that need water they aren't in.</summary>
    public static IReadOnlyList<EmoteInfo> Playable(IReadOnlyList<EmoteInfo> unlocked, bool swimming, bool diving) =>
        unlocked.Where(emote => (swimming || diving || !Swimming.Contains(emote.Command))
            && (diving || !Underwater.Contains(emote.Command))).ToList();
}
