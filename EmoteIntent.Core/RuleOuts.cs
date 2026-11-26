using System.Reflection;

namespace EmoteIntent.Core;

/// <summary>
/// Data/rule-outs.txt: emotes a text can't mean when it has certain words ("cold" rules out Tremble), unless it also
/// has others ("cold and scared" keeps it). For meanings that sharing words can't tell apart.
/// </summary>
public sealed class RuleOuts
{
    private const string Resource = "EmoteIntent.rule-outs.txt";

    private readonly IReadOnlyList<Rule> rules;

    private RuleOuts(IReadOnlyList<Rule> rules) => this.rules = rules;

    /// <summary>The rules shipped with the plugin.</summary>
    public static RuleOuts LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidDataException($"Missing resource {Resource}");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Rules from their text: "words | keeping words | /emotes" per line, # for comments.</summary>
    public static RuleOuts Parse(string text)
    {
        var rules = new List<Rule>();
        foreach (var line in text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')))
        {
            var parts = line.Split('|').Select(part => part.Trim()).ToArray();
            if (parts.Length != 3 || parts[0].Length == 0 || parts[2].Length == 0)
            {
                throw new InvalidDataException($"Not \"words | keeping words | /emotes\": {line}");
            }

            rules.Add(new Rule(
                TextNormalizer.Words(parts[0]).ToHashSet(),
                TextNormalizer.Words(parts[1]).ToHashSet(),
                parts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase)));
        }

        return new RuleOuts(rules);
    }

    /// <summary>Every emote the rules name, to check they are real.</summary>
    public IEnumerable<string> Commands => rules.SelectMany(rule => rule.Emotes);

    /// <summary>The emotes <paramref name="text"/> rules out; empty when no rule applies.</summary>
    public IReadOnlySet<string> RuledOut(string text)
    {
        var words = TextNormalizer.Words(text).ToHashSet();
        var ruledOut = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules.Where(rule => rule.Words.Overlaps(words) && !rule.Keeping.Overlaps(words)))
        {
            ruledOut.UnionWith(rule.Emotes);
        }

        return ruledOut;
    }

    private sealed record Rule(HashSet<string> Words, HashSet<string> Keeping, HashSet<string> Emotes);
}
