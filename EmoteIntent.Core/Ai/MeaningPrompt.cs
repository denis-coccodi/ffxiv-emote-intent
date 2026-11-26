using System.Text.Json;
using System.Text.Json.Nodes;

namespace EmoteIntent.Core.Ai;

/// <summary>
/// Asks a model only what the player means, in a few plain words ("the boss one-shot me again" → "dead",
/// "frustrated"), and lets the phrase bank turn those words into emotes: small models paraphrase well but choose
/// badly among hundreds of emotes, while the phrase bank knows plain words well. A short prompt, so it's fast.
/// </summary>
public static class MeaningPrompt
{
    /// <summary>How many plain phrases the model may give, most fitting first.</summary>
    public const int MaxMeanings = 3;

    /// <summary>The system message, the same for every request.</summary>
    public const string SystemMessage =
        """
        A Final Fantasy XIV player typed /feel followed by a message, to play an emote (a character animation).
        Say what feeling, reaction or action the message expresses, as 1 to 3 very short plain phrases of 1 to 3
        everyday words each, most fitting first. Use simple words like happy, sad, angry, tired, scared, laughing,
        confused, celebrate, thank you, hello, goodbye, dance, eat, drink, cold, hot.
        Examples:
        "my cat knocked my coffee off the desk again" → ["annoyed", "facepalm"]
        "finally friday!!" → ["celebrate", "happy"]
        "brrr my fingers are numb" → ["cold", "shiver"]
        "see ya tomorrow guys" → ["goodbye", "wave"]
        Answer with JSON only: {"meanings": ["...", "..."]}.
        """;

    /// <summary>The user message for what the player typed.</summary>
    public static string User(string text) => $"/feel {text.Trim()}";

    /// <summary>The JSON schema of the answer: 1 to <see cref="MaxMeanings"/> short strings.</summary>
    public static JsonObject Schema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["meanings"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string" },
                ["minItems"] = 1,
                ["maxItems"] = MaxMeanings,
            },
        },
        ["required"] = new JsonArray("meanings"),
    };

    /// <summary>The plain phrases in a model's answer, trimmed, at most <see cref="MaxMeanings"/> of 1-40 characters.</summary>
    public static IReadOnlyList<string> ParseAnswer(string json)
    {
        try
        {
            if (JsonNode.Parse(json)?["meanings"] is not JsonArray meanings)
            {
                return [];
            }

            return meanings
                .Select(meaning => meaning?.GetValueKind() == JsonValueKind.String ? meaning.GetValue<string>().Trim() : null)
                .OfType<string>()
                .Where(meaning => meaning.Length is > 0 and <= 40)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxMeanings)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
