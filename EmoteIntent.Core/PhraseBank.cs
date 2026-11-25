using System.Reflection;

namespace EmoteIntent.Core;

/// <summary>
/// The ways players say what each emote means, by emote command ("/hug" → "give you a hug", ...).
/// Shipped inside EmoteIntent.Core from Data/Phrases/&lt;language&gt;/&lt;command&gt;.txt: one phrase per line,
/// lines starting with # are comments.
/// </summary>
public sealed class PhraseBank
{
    private const string ResourcePrefix = "EmoteIntent.Phrases.";

    private PhraseBank(IReadOnlyDictionary<string, IReadOnlyList<string>> phrases) => Phrases = phrases;

    /// <summary>The phrases of each emote command, the command with its slash.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Phrases { get; }

    /// <summary>The phrase bank shipped with the plugin, for a language ("en").</summary>
    public static PhraseBank LoadEmbedded(string language = "en")
    {
        var assembly = typeof(PhraseBank).Assembly;
        var prefix = $"{ResourcePrefix}{language}.";
        var files = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(name => (Command: "/" + name[prefix.Length..^".txt".Length], Text: Read(assembly, name)));
        return Parse(files);
    }

    /// <summary>A phrase bank from (command, file text) pairs; the command with or without its slash.</summary>
    public static PhraseBank Parse(IEnumerable<(string Command, string Text)> files)
    {
        var phrases = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (command, text) in files)
        {
            var lines = text.Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
                .ToList();
            phrases["/" + command.TrimStart('/')] = lines;
        }

        return new PhraseBank(phrases);
    }

    private static string Read(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException($"Missing phrase bank resource {resource}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
