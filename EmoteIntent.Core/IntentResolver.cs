namespace EmoteIntent.Core;

/// <summary>What to do with what the player typed: play an emote, suggest some, or say nothing fits.</summary>
public abstract record IntentResult
{
    private IntentResult()
    {
    }

    /// <summary>
    /// Play this emote: the player named it, or one emote clearly fits. <see cref="AlsoFits"/> lists other emotes that
    /// fit well too ("book" plays Read; Carry Book and Reference also fit), to offer, never to play.
    /// </summary>
    public sealed record Play(EmoteInfo Emote) : IntentResult
    {
        /// <summary>Other emotes that fit well, best first; empty when none do.</summary>
        public IReadOnlyList<EmoteInfo> AlsoFits { get; init; } = [];
    }

    /// <summary>Several emotes might fit: show them and let the player choose. Never played on its own.</summary>
    public sealed record Suggest(IReadOnlyList<EmoteInfo> Emotes) : IntentResult;

    /// <summary>No unlocked emote fits.</summary>
    public sealed record NoMatch : IntentResult;
}

/// <summary>Decides which emote, if any, fits what the player typed, among the emotes they have unlocked.</summary>
public sealed class IntentResolver(PhraseMatcher matcher)
{
    /// <summary>A match at least this good plays at once when no other emote comes close.</summary>
    public const double PlayScore = 0.6;

    /// <summary>A match of <see cref="PlayScore"/> must beat the second by this much to play at once.</summary>
    public const double PlayLead = 0.15;

    /// <summary>The lead a near-perfect match (0.95) needs: the stronger the match, the smaller the lead it needs.</summary>
    public const double MinPlayLead = 0.05;

    /// <summary>The best score a match that isn't a phrase typed exactly can reach.</summary>
    private const double BestInexactScore = 0.95;

    /// <summary>Matches below this aren't worth suggesting.</summary>
    public const double SuggestScore = 0.35;

    /// <summary>How many emotes a suggestion offers at most.</summary>
    public const int SuggestCount = 3;

    /// <summary>Next to an emote that plays, other matches this good are offered too ("sad" plays Slump; Cry also fits).</summary>
    public const double AlsoFitsScore = 0.6;

    /// <summary>What to do with <paramref name="text"/>, given the character's unlocked emotes.</summary>
    public IntentResult Resolve(string text, IReadOnlyList<EmoteInfo> unlocked)
    {
        var byCommand = unlocked
            .GroupBy(emote => emote.Command, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        // An emote named outright plays, unless the name is also a feeling whose preference list holds that emote as
        // a fallback ("sad": Slump first, the Sad frown only without it): then the list's first unlocked emote plays.
        if (EmoteNameMatcher.Match(text, unlocked) is { } named)
        {
            var ranked = matcher.Rank(text, byCommand.Keys, 1);
            var feeling = ranked.Count > 0 ? ranked[0] : null;
            var preferred = feeling is { Score: >= 1 } && feeling.Commands.Count > 1
                && feeling.Commands.Contains(named.Command, StringComparer.OrdinalIgnoreCase);
            return new IntentResult.Play(preferred ? byCommand[feeling!.Command] : named);
        }
        var matches = matcher.Rank(text, byCommand.Keys)
            .Where(match => match.Score >= SuggestScore)
            .ToList();
        if (matches.Count == 0)
        {
            return new IntentResult.NoMatch();
        }

        // An exact phrase of one emote alone plays; otherwise the best must be good and clearly ahead.
        var second = matches.Count > 1 ? matches[1].Score : 0;
        var exact = matches[0].Score >= 1 && second < 1;
        if (exact || (matches[0].Score >= PlayScore && matches[0].Score - second >= RequiredLead(matches[0].Score)))
        {
            // Not the emotes of the played one's own family or feeling list: they're the same gesture or a fallback.
            var alsoFits = matches.Skip(1)
                .Where(match => match.Score >= AlsoFitsScore
                    && !matches[0].Commands.Contains(match.Command, StringComparer.OrdinalIgnoreCase))
                .Take(SuggestCount)
                .Select(match => byCommand[match.Command])
                .ToList();
            return new IntentResult.Play(byCommand[matches[0].Command]) { AlsoFits = alsoFits };
        }

        return new IntentResult.Suggest(matches.Take(SuggestCount).Select(match => byCommand[match.Command]).ToList());
    }

    /// <summary>
    /// How far ahead of the second a match must be to play: <see cref="PlayLead"/> at <see cref="PlayScore"/>, down to
    /// <see cref="MinPlayLead"/> for a near-perfect one. "hand on my heart" (0.82, 0.14 ahead) plays; a borderline
    /// 0.6 still needs a clear lead.
    /// </summary>
    public static double RequiredLead(double score)
    {
        var strength = Math.Clamp((score - PlayScore) / (BestInexactScore - PlayScore), 0, 1);
        return PlayLead - (strength * (PlayLead - MinPlayLead));
    }
}
