using System.Text;
using System.Text.Json.Nodes;

namespace EmoteIntent.Core.Ai;

/// <summary>
/// What an AI model is asked: instructions and every emote's guide line (the same for every request, so a local model
/// can reuse it from its cache), the player's text, and a JSON schema that only allows the character's unlocked
/// commands, so the model can't answer with an emote that doesn't exist or isn't unlocked.
/// </summary>
public sealed class EmotePrompt
{
    /// <summary>How many emotes the model may suggest, best first.</summary>
    public const int MaxEmotes = 3;

    private const string Instructions =
        """
        You choose Final Fantasy XIV emotes. A player typed /feel followed by what they mean: a feeling, a reaction,
        an action, chat shorthand or a whole sentence. Choose the emotes that best express it, best first, at most 3.
        - Match the meaning, not the words: "dead tired" means tired, "I'm dead" after a joke means laughing.
        - For strong feelings, prefer an emote with the whole body over a face-only expression.
        - If the text names an emote or its command, choose that emote first.
        - Only list emotes that really fit; one is fine.
        Answer with JSON only: {"emotes": ["/command", ...]}.
        The emotes (command | what the character does | when it fits):
        """;

    private readonly string system;

    /// <summary>Prepares the instructions and the guide's lines, in the guide's order.</summary>
    public EmotePrompt(EmoteGuide guide)
    {
        var builder = new StringBuilder(Instructions.ReplaceLineEndings("\n")).Append('\n');
        foreach (var entry in guide.Entries.Values)
        {
            builder.Append(entry.Command).Append(" | ").Append(entry.Shows).Append(" | ").Append(entry.Fits).Append('\n');
        }

        system = builder.ToString();
    }

    /// <summary>The system message: instructions and the whole guide, the same for every request.</summary>
    public string SystemMessage => system;

    /// <summary>The user message for what the player typed.</summary>
    public static string User(string text) => $"The player typed: /feel {text.Trim()}";

    /// <summary>
    /// The JSON schema of the answer: {"emotes": [...]} with each item one of <paramref name="commands"/> (the
    /// character's unlocked emotes), at least one and at most <see cref="MaxEmotes"/>.
    /// </summary>
    public static JsonObject Schema(IEnumerable<string> commands)
    {
        var allowed = new JsonArray();
        foreach (var command in commands.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            allowed.Add(command);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["emotes"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string", ["enum"] = allowed },
                    ["minItems"] = 1,
                    ["maxItems"] = MaxEmotes,
                },
            },
            ["required"] = new JsonArray("emotes"),
        };
    }

    /// <summary>
    /// The commands in a model's answer that are among <paramref name="commands"/>, in its order, once each, at most
    /// <see cref="MaxEmotes"/>; empty when the answer isn't the expected JSON. Never trusts the model blindly.
    /// </summary>
    public static IReadOnlyList<string> ParseAnswer(string json, IEnumerable<string> commands)
    {
        var allowed = commands.ToHashSet(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (JsonNode.Parse(json)?["emotes"] is not JsonArray emotes)
            {
                return [];
            }

            return emotes.Select(emote => emote?.GetValueKind() == System.Text.Json.JsonValueKind.String ? emote.GetValue<string>() : null)
                .OfType<string>()
                .Select(command => command.Trim())
                .Select(command => command.StartsWith('/') ? command : "/" + command)
                .Where(allowed.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxEmotes)
                .ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}
