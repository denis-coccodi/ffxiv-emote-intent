namespace EmoteIntent.Core;

/// <summary>What to do with what the player typed: play an emote, suggest some, or say nothing fits.</summary>
public abstract record IntentResult
{
    private IntentResult()
    {
    }

    /// <summary>Play this emote: the player named it, or one emote clearly fits.</summary>
    public sealed record Play(EmoteInfo Emote) : IntentResult;

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

    /// <summary>The best match must beat the second by this much to play at once.</summary>
    public const double PlayLead = 0.15;

    /// <summary>Matches below this aren't worth suggesting.</summary>
    public const double SuggestScore = 0.35;

    /// <summary>How many emotes a suggestion offers at most.</summary>
    public const int SuggestCount = 3;

    /// <summary>What to do with <paramref name="text"/>, given the character's unlocked emotes.</summary>
    public IntentResult Resolve(string text, IReadOnlyList<EmoteInfo> unlocked)
    {
        if (EmoteNameMatcher.Match(text, unlocked) is { } named)
        {
            return new IntentResult.Play(named);
        }

        var byCommand = unlocked
            .GroupBy(emote => emote.Command, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
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
        if (exact || (matches[0].Score >= PlayScore && matches[0].Score - second >= PlayLead))
        {
            return new IntentResult.Play(byCommand[matches[0].Command]);
        }

        return new IntentResult.Suggest(matches.Take(SuggestCount).Select(match => byCommand[match.Command]).ToList());
    }
}
