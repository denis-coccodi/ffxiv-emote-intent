using System.Text;

namespace EmoteIntent.Core;

/// <summary>Turns what a player types, and the phrase bank's phrases, into comparable text and words.</summary>
public static class TextNormalizer
{
    /// <summary>The longest chunk with a symbol that still counts as an emoticon (">///&lt;" is five).</summary>
    private const int MaxEmoticonLength = 5;

    /// <summary>
    /// The text lowercased, apostrophes dropped ("I'm" → "im") and spaces collapsed, keeping symbols so that
    /// emoticons such as "o/", "&lt;3" or "^^" still compare equal.
    /// </summary>
    public static string Compact(string text) =>
        string.Join(' ', text.ToLowerInvariant().Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace("’", string.Empty, StringComparison.Ordinal)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The words of the text: runs of letters and digits, with a plural "s" dropped from longer words
    /// ("hugs" → "hug"). Sentence punctuation at a chunk's ends is ignored ("hi!" → "hi"); a short chunk that
    /// still holds a symbol is an emoticon and stays whole ("&lt;3", ":d", "o/", "10/10").
    /// </summary>
    public static IReadOnlyList<string> Words(string text)
    {
        var words = new List<string>();
        foreach (var raw in Compact(text).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var chunk = raw.Trim('.', ',', '!', '?', '"', ';');
            if (chunk.Length == 0)
            {
                chunk = raw;
            }

            if (chunk.Length <= MaxEmoticonLength && !chunk.All(char.IsLetterOrDigit))
            {
                words.Add(chunk);
                continue;
            }

            var before = words.Count;
            var word = new StringBuilder();
            foreach (var c in chunk)
            {
                if (char.IsLetterOrDigit(c))
                {
                    word.Append(c);
                }
                else
                {
                    Flush(word, words);
                }
            }

            Flush(word, words);
            if (words.Count == before)
            {
                words.Add(chunk);
            }
        }

        return words;
    }

    private static void Flush(StringBuilder word, List<string> words)
    {
        if (word.Length == 0)
        {
            return;
        }

        var text = word.ToString();
        if (text.Length > 3 && text.EndsWith('s') && !text.EndsWith("ss", StringComparison.Ordinal))
        {
            text = text[..^1];
        }

        words.Add(text);
        word.Clear();
    }
}
