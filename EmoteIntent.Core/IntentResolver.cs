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
        /// <summary>Other emotes that fit well (<see cref="IntentResolver.AlsoFitsScore"/>), best first; often empty.</summary>
        public IReadOnlyList<EmoteInfo> AlsoFits { get; init; } = [];

        /// <summary>
        /// Every other emote worth suggesting (<see cref="IntentResolver.SuggestScore"/>), best first. Includes
        /// <see cref="AlsoFits"/>.
        /// </summary>
        public IReadOnlyList<EmoteInfo> Alternatives { get; init; } = [];

        /// <summary>The player typed this emote's name or command ("hug", "/hug"): it's the one they mean.</summary>
        public bool Named { get; init; }
    }

    /// <summary>Several emotes might fit: show them and let the player choose. Never played on its own.</summary>
    public sealed record Suggest(IReadOnlyList<EmoteInfo> Emotes) : IntentResult;

    /// <summary>
    /// The player named an emote they haven't unlocked: offer the ones they have that come closest. Never played on
    /// its own; <see cref="Closest"/> may be empty.
    /// </summary>
    public sealed record Locked(EmoteInfo Wanted, IReadOnlyList<EmoteInfo> Closest) : IntentResult;

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

    /// <summary>How many of a locked emote's phrases are used to find the closest unlocked ones.</summary>
    private const int ClosestPhraseCount = 20;

    /// <summary>
    /// What to do with <paramref name="text"/>, given the character's unlocked emotes. With <paramref name="all"/>
    /// (every emote of the game), naming one that isn't unlocked offers the closest ones that are.
    /// </summary>
    public IntentResult Resolve(string text, IReadOnlyList<EmoteInfo> unlocked, IReadOnlyList<EmoteInfo>? all = null)
    {
        var byCommand = unlocked
            .GroupBy(emote => emote.Command, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var matches = matcher.Rank(text, byCommand.Keys)
            .Where(match => match.Score >= SuggestScore)
            .ToList();

        // An emote named outright plays, unless the name is also a feeling whose preference list holds that emote as
        // a fallback ("sad": Slump first, the Sad frown only without it): then the list's first unlocked emote plays.
        if (EmoteNameMatcher.Match(text, unlocked) is { } named)
        {
            var feeling = matches.Count > 0 ? matches[0] : null;
            var preferred = feeling is { Score: >= 1 } && feeling.Commands.Count > 1
                && feeling.Commands.Contains(named.Command, StringComparer.OrdinalIgnoreCase);
            return preferred
                ? Played(byCommand[feeling!.Command], feeling.Commands, matches, byCommand)
                : Played(named, [named.Command], matches, byCommand) with { Named = true };
        }

        // An emote named outright that the character doesn't have: the closest ones they do have, to pick from. Not
        // when the name is a feeling whose list holds it ("sad" with only Slump unlocked plays Slump).
        if (all is not null && EmoteNameMatcher.Match(text, all) is { } wanted && !byCommand.ContainsKey(wanted.Command))
        {
            var feeling = matches.Count > 0 ? matches[0] : null;
            var listed = feeling is { Score: >= 1 } && feeling.Commands.Contains(wanted.Command, StringComparer.OrdinalIgnoreCase);
            if (!listed)
            {
                return new IntentResult.Locked(wanted, Closest(wanted, byCommand));
            }
        }

        if (matches.Count == 0)
        {
            return new IntentResult.NoMatch();
        }

        // An exact phrase of one emote alone plays; otherwise the best must be good and clearly ahead.
        var second = matches.Count > 1 ? matches[1].Score : 0;
        var exact = matches[0].Score >= 1 && second < 1;
        if (exact || (matches[0].Score >= PlayScore && matches[0].Score - second >= RequiredLead(matches[0].Score)))
        {
            return Played(byCommand[matches[0].Command], matches[0].Commands, matches, byCommand);
        }

        return new IntentResult.Suggest(matches.Take(SuggestCount).Select(match => byCommand[match.Command]).ToList());
    }

    // The unlocked emotes closest to a locked one: each of its phrases (and its name and description) votes for the
    // unlocked emotes it matches, by score; the ones with the most votes come first.
    private List<EmoteInfo> Closest(EmoteInfo wanted, Dictionary<string, EmoteInfo> byCommand) =>
        matcher.PhrasesOf(wanted.Command).Take(ClosestPhraseCount)
            .Append(wanted.Name)
            .Append(wanted.Description)
            .Where(phrase => phrase.Length > 0)
            .SelectMany(phrase => matcher.Rank(phrase, byCommand.Keys).Where(match => match.Score >= SuggestScore))
            .GroupBy(match => match.Command, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Sum(match => match.Score))
            .Take(SuggestCount)
            .Select(group => byCommand[group.Key])
            .ToList();

    // Plays an emote, with the other matches as alternatives: not the emotes of its own family or feeling list
    // (the same gesture, or a fallback), and those scoring AlsoFitsScore or more as AlsoFits too.
    private static IntentResult.Play Played(
        EmoteInfo emote, IReadOnlyList<string> own, List<PhraseMatch> matches, Dictionary<string, EmoteInfo> byCommand)
    {
        var others = matches
            .Where(match => !own.Contains(match.Command, StringComparer.OrdinalIgnoreCase)
                && !string.Equals(match.Command, emote.Command, StringComparison.OrdinalIgnoreCase))
            .Take(SuggestCount)
            .ToList();
        return new IntentResult.Play(emote)
        {
            Alternatives = others.Select(match => byCommand[match.Command]).ToList(),
            AlsoFits = others.Where(match => match.Score >= AlsoFitsScore).Select(match => byCommand[match.Command]).ToList(),
        };
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
