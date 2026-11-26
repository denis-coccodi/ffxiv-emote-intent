namespace EmoteIntent.Core;

/// <summary>A word the player typed that isn't in the phrase bank, and the bank's word it was read as.</summary>
public sealed record Correction(string Typed, string Read);

/// <summary>
/// Fixes typos against the phrase bank's own words, like a phone keyboard's autocorrect: a word the bank doesn't know
/// becomes the closest one it does ("exicted" → "excited", "helo" → "hello", "spooookyy" → "spooky"). Words it knows
/// never change, and short words aren't touched ("hum" stays "hum", not "hug"). No AI: letters added, dropped,
/// swapped, changed or held down too long.
/// </summary>
public sealed class SpellCorrector
{
    /// <summary>Words shorter than this are left alone: one changed letter is too often another real word.</summary>
    public const int MinLength = 4;

    /// <summary>Words this long or longer may have two typos ("embarassed"); shorter ones one.</summary>
    public const int TwoTyposLength = 8;

    // Every word of the phrases as written ("jealous", not the matcher's "jealou"), and how often it appears.
    private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);

    // The commonest word (3 letters or more) for each spelling with repeated letters squashed ("hello" and "helo" are
    // both "helo"; "hugg" is "hug").
    private readonly Dictionary<string, string> bySquashed = new(StringComparer.Ordinal);

    /// <summary>Learns the words of <paramref name="phrases"/>, the ones a typo may be corrected to.</summary>
    public SpellCorrector(IEnumerable<string> phrases)
    {
        foreach (var word in phrases.SelectMany(Letters))
        {
            counts[word] = counts.GetValueOrDefault(word) + 1;
        }

        foreach (var (word, count) in counts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (word.Length >= MinLength - 1)
            {
                bySquashed.TryAdd(Squash(word), word);
            }
        }
    }

    /// <summary>
    /// <paramref name="text"/> in compact form with each word the bank doesn't know replaced by its closest known word,
    /// and what was replaced; the compact text and no corrections when nothing needed fixing.
    /// </summary>
    public (string Text, IReadOnlyList<Correction> Corrections) Correct(string text)
    {
        var corrections = new List<Correction>();
        var chunks = TextNormalizer.Compact(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < chunks.Length; i++)
        {
            var core = chunks[i].Trim('.', ',', '!', '?', '"', ';');
            if (core.Length < MinLength || !core.All(char.IsLetter) || Knows(core) || Closest(core) is not { } read)
            {
                continue;
            }

            chunks[i] = chunks[i].Replace(core, read, StringComparison.Ordinal);
            corrections.Add(new Correction(core, read));
        }

        return (string.Join(' ', chunks), corrections);
    }

    // Known as written, or with a plural "s" the phrases don't have ("hugs" for "hug").
    private bool Knows(string word) =>
        counts.ContainsKey(word) || (word.EndsWith('s') && counts.ContainsKey(word[..^1]));

    // Letters held down too long first ("happpy", "helo"); else the known word fewest typos away within the allowance,
    // on a tie the commonest. Typos rarely hit the first letter, so a word that starts differently gets one typo at most.
    private string? Closest(string typed)
    {
        if (bySquashed.TryGetValue(Squash(typed), out var held))
        {
            return held;
        }

        var allowed = typed.Length >= TwoTyposLength ? 2 : 1;
        string? best = null;
        var bestDistance = int.MaxValue;
        var bestCount = 0;
        foreach (var (word, count) in counts)
        {
            var limit = word[0] == typed[0] ? allowed : 1;
            if (word.Length < MinLength || Math.Abs(word.Length - typed.Length) > limit)
            {
                continue;
            }

            var distance = Distance(typed, word, limit);
            if (distance <= limit && (distance < bestDistance || (distance == bestDistance
                && (count > bestCount || (count == bestCount && string.CompareOrdinal(word, best) < 0)))))
            {
                best = word;
                bestDistance = distance;
                bestCount = count;
            }
        }

        return best;
    }

    /// <summary>
    /// How many typos turn <paramref name="a"/> into <paramref name="b"/>: letters added, dropped, changed, or two
    /// neighbours swapped ("exicted" → "excited" is one). Stops counting past <paramref name="limit"/>.
    /// </summary>
    public static int Distance(string a, string b, int limit = int.MaxValue)
    {
        var beforePrevious = new int[b.Length + 1];
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            var rowBest = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                {
                    current[j] = Math.Min(current[j], beforePrevious[j - 2] + 1);
                }

                rowBest = Math.Min(rowBest, current[j]);
            }

            if (rowBest > limit)
            {
                return limit + 1;
            }

            (beforePrevious, previous, current) = (previous, current, beforePrevious);
        }

        return previous[b.Length];
    }

    // The word with each run of one letter cut to one ("spooookyy" → "spoky").
    private static string Squash(string word) =>
        new(word.Where((c, i) => i == 0 || c != word[i - 1]).ToArray());

    // The runs of letters in a phrase, lowercased, apostrophes dropped ("I'm" → "im").
    private static IEnumerable<string> Letters(string phrase) =>
        string.Concat(TextNormalizer.Compact(phrase).Select(c => char.IsLetter(c) ? c : ' '))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
