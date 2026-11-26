using System.Reflection;

namespace EmoteIntent.Core;

/// <summary>Emotes that go together ("Eat": every eating emote), listed whole by "/feel all eat".</summary>
/// <param name="Name">What it's called in chat ("Eat").</param>
/// <param name="Names">Every name it answers to, compact ("eat", "eating", "food"…), its own name included.</param>
/// <param name="Commands">Its emotes, in the order they're listed.</param>
public sealed record EmoteBundle(string Name, IReadOnlyList<string> Names, IReadOnlyList<string> Commands);

/// <summary>
/// The bundles shipped in Data/bundles.txt, and how players ask for one: "all" and its name ("all eat",
/// "all the jumps"), or "all" alone for the list of bundles.
/// </summary>
public sealed class EmoteBundles
{
    private const string Resource = "EmoteIntent.bundles.txt";

    // What asks for a whole bundle, longest first so "all the" wins over "all".
    private static readonly string[] AllWords = ["all the", "all of the", "every", "all"];

    private EmoteBundles(IReadOnlyList<EmoteBundle> bundles) => All = bundles;

    /// <summary>Every bundle, in the file's order.</summary>
    public IReadOnlyList<EmoteBundle> All { get; }

    /// <summary>The bundles shipped with the plugin.</summary>
    public static EmoteBundles LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidDataException($"Missing resource {Resource}");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Bundles from the text of a bundles file: "Name | other, names | /a /b /c" per line, # for comments.</summary>
    public static EmoteBundles Parse(string text)
    {
        var bundles = new List<EmoteBundle>();
        foreach (var line in text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')))
        {
            var parts = line.Split('|');
            if (parts.Length != 3)
            {
                throw new InvalidDataException($"A bundle needs a name, other names and emotes: {line}");
            }

            var name = parts[0].Trim();
            var names = parts[1].Split(',').Prepend(name)
                .Select(TextNormalizer.Compact)
                .Where(other => other.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var commands = parts[2].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
            bundles.Add(new EmoteBundle(name, names, commands));
        }

        return new EmoteBundles(bundles);
    }

    /// <summary>The bundle with this name ("eat", "Jumps"), or null.</summary>
    public EmoteBundle? Find(string name)
    {
        var wanted = Clean(name);
        return All.FirstOrDefault(bundle => bundle.Names.Contains(wanted, StringComparer.Ordinal));
    }

    /// <summary>The first bundle <paramref name="command"/> belongs to ("/eatpizza": Eat), or null.</summary>
    public EmoteBundle? Of(string command) =>
        All.FirstOrDefault(bundle => bundle.Commands.Contains(command, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Whether <paramref name="text"/> asks for a bundle: "all eat" or "all the jumps" gives that bundle, "all" alone
    /// asks for the list of bundles (<see cref="BundleRequest.Bundle"/> null). Null when it doesn't ("all good",
    /// "all right": "all" with words that aren't a bundle's name).
    /// </summary>
    public BundleRequest? Request(string text)
    {
        var compact = Clean(text);
        foreach (var all in AllWords)
        {
            if (compact == all)
            {
                return new BundleRequest(null);
            }

            if (compact.StartsWith(all + " ", StringComparison.Ordinal) && Find(compact[(all.Length + 1)..]) is { } bundle)
            {
                return new BundleRequest(bundle);
            }
        }

        return null;
    }

    /// <summary>
    /// The emotes of <paramref name="bundle"/> among <paramref name="unlocked"/>, in the bundle's order, once each
    /// (the three Grand Company salutes share /gcsalute).
    /// </summary>
    public static IReadOnlyList<EmoteInfo> UnlockedOf(EmoteBundle bundle, IEnumerable<EmoteInfo> unlocked)
    {
        var byCommand = unlocked
            .GroupBy(emote => emote.Command, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        return bundle.Commands.Where(byCommand.ContainsKey).Select(command => byCommand[command]).ToList();
    }

    // Compact, without sentence punctuation at the end ("all jumps!" is "all jumps").
    private static string Clean(string text) => TextNormalizer.Compact(text).Trim('.', ',', '!', '?', ';', ' ');
}

/// <summary>A request for a whole bundle; <paramref name="Bundle"/> null asks for the list of bundles.</summary>
public sealed record BundleRequest(EmoteBundle? Bundle);
