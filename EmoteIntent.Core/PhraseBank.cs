using System.Reflection;

namespace EmoteIntent.Core;

/// <summary>
/// The ways players say what each emote means ("/hug" → "give you a hug", ...). Shipped inside EmoteIntent.Core from
/// Data/Phrases/&lt;language&gt;/&lt;name&gt;.txt: one phrase per line, lines starting with # are comments. A file is
/// one emote, "/&lt;name&gt;", unless a line "@ /a /b /c" makes it a family of near-identical emotes (Jump for Joy
/// 1-5, the penlight colours…) that share its phrases: whichever the player has unlocked plays.
/// </summary>
public sealed class PhraseBank
{
    private const string ResourcePrefix = "EmoteIntent.Phrases.";
    private const char FamilyMarker = '@';

    private PhraseBank(
        IReadOnlyDictionary<string, IReadOnlyList<string>> phrases, IReadOnlyDictionary<string, IReadOnlyList<string>> commands)
    {
        Phrases = phrases;
        Commands = commands;
    }

    /// <summary>The phrases of each file, by its key: "/" and the file name ("/hug", "/jumpforjoy").</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Phrases { get; }

    /// <summary>The emote commands each key plays, in order of preference: just the key, or a family's members.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Commands { get; }

    /// <summary>The phrase bank shipped with the plugin, for a language ("en").</summary>
    public static PhraseBank LoadEmbedded(string language = "en")
    {
        var assembly = typeof(PhraseBank).Assembly;
        var prefix = $"{ResourcePrefix}{language}.";
        var files = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(name => (Name: name[prefix.Length..^".txt".Length], Text: Read(assembly, name)));
        return Parse(files);
    }

    /// <summary>A phrase bank from (file name, file text) pairs; the name with or without a leading slash.</summary>
    public static PhraseBank Parse(IEnumerable<(string Name, string Text)> files)
    {
        var phrases = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var commands = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, text) in files)
        {
            var key = "/" + name.TrimStart('/');
            var lines = text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
            var family = lines.FirstOrDefault(line => line.StartsWith(FamilyMarker));
            phrases[key] = lines.Where(line => !line.StartsWith('#') && !line.StartsWith(FamilyMarker)).ToList();
            commands[key] = family is null
                ? [key]
                : family[1..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        return new PhraseBank(phrases, commands);
    }

    private static string Read(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException($"Missing phrase bank resource {resource}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
