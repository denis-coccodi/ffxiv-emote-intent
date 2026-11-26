using System.IO.Compression;
using System.Reflection;

namespace EmoteIntent.Core;

/// <summary>
/// The ENABLE word list (public domain, about 173,000 English words with their inflections), so typo correction can
/// tell a real word the phrase bank doesn't know ("raining") from a misspelling ("rainign"). Loaded once, on first use.
/// </summary>
public static class EnglishWords
{
    private const string Resource = "EmoteIntent.english-words.txt.gz";

    private static readonly Lazy<HashSet<string>> Words = new(Load);

    /// <summary>Whether <paramref name="word"/> (lowercase) is an English word.</summary>
    public static bool Contains(string word) => Words.Value.Contains(word);

    private static HashSet<string> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidDataException($"Missing resource {Resource}");
        using var unzipped = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(unzipped);
        var words = new HashSet<string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0)
            {
                words.Add(line);
            }
        }

        return words;
    }
}
