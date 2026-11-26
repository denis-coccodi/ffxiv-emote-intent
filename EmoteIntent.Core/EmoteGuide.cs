using System.Reflection;

namespace EmoteIntent.Core;

/// <summary>What one emote visibly does, and when players use it.</summary>
/// <param name="Command">The emote's command ("/bow").</param>
/// <param name="Shows">What the character does ("Bows politely from the waist.").</param>
/// <param name="Fits">When it fits ("Thanking someone, a formal greeting…").</param>
public sealed record EmoteGuideEntry(string Command, string Shows, string Fits);

/// <summary>
/// Data/emote-guide.txt: an in-depth line per emote, written for this project, for an AI model choosing among the
/// emotes (the game's own descriptions are often too short to tell them apart).
/// </summary>
public sealed class EmoteGuide
{
    private const string Resource = "EmoteIntent.emote-guide.txt";

    private EmoteGuide(IReadOnlyDictionary<string, EmoteGuideEntry> entries) => Entries = entries;

    /// <summary>Every emote's entry, by command.</summary>
    public IReadOnlyDictionary<string, EmoteGuideEntry> Entries { get; }

    /// <summary>The guide shipped with the plugin.</summary>
    public static EmoteGuide LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidDataException($"Missing resource {Resource}");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>A guide from its text: "/command | shows | fits" per line, # for comments.</summary>
    public static EmoteGuide Parse(string text)
    {
        var entries = new Dictionary<string, EmoteGuideEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')))
        {
            var parts = line.Split('|').Select(part => part.Trim()).ToArray();
            if (parts.Length != 3 || parts.Any(part => part.Length == 0))
            {
                throw new InvalidDataException($"A guide line needs a command, what it shows and when it fits: {line}");
            }

            entries[parts[0]] = new EmoteGuideEntry(parts[0], parts[1], parts[2]);
        }

        return new EmoteGuide(entries);
    }
}
