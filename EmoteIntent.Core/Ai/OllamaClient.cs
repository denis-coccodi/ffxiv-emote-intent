using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace EmoteIntent.Core.Ai;

/// <summary>A model's answer: the emotes it chose, best first (possibly none), and how long it took.</summary>
public sealed record AiAnswer(IReadOnlyList<string> Commands, TimeSpan Took);

/// <summary>
/// Asks a model running in Ollama on the player's own PC (http://127.0.0.1:11434 by default) to choose emotes.
/// Free and private: nothing leaves the PC. Only used when the player switched it on. The one place that talks to
/// the network, which is why it may use HttpClient (banned everywhere else, see BannedSymbols.txt).
/// </summary>
public sealed class OllamaClient
{
    /// <summary>Where Ollama listens unless told otherwise.</summary>
    public static readonly Uri DefaultAddress = new("http://127.0.0.1:11434/");

#pragma warning disable RS0030 // The opted-in AI client is the only code allowed to make network calls.
    private readonly HttpClient http;

    /// <summary>A client using <paramref name="http"/> (its BaseAddress is Ollama's address).</summary>
    public OllamaClient(HttpClient http, EmotePrompt prompt, string model)
#pragma warning restore RS0030
    {
        this.http = http;
        Prompt = prompt;
        Model = model;
    }

    /// <summary>The Ollama model asked ("qwen3:4b").</summary>
    public string Model { get; }

    /// <summary>The instructions and guide sent with every request.</summary>
    public EmotePrompt Prompt { get; }

    /// <summary>
    /// The model's choice for <paramref name="text"/> among <paramref name="commands"/> (the unlocked emotes). Throws
    /// <see cref="HttpRequestException"/> when Ollama isn't running or doesn't have the model.
    /// </summary>
    public async Task<AiAnswer> PickAsync(string text, IReadOnlyCollection<string> commands, CancellationToken cancellation)
    {
        var request = new JsonObject
        {
            ["model"] = Model,
            ["stream"] = false,
            // Thinking models (qwen3…) would think aloud first: slower, and this is a quick choice.
            ["think"] = false,
            // Keep the model and the cached guide loaded between /feel commands.
            ["keep_alive"] = "30m",
            ["format"] = EmotePrompt.Schema(commands),
            ["options"] = new JsonObject { ["temperature"] = 0, ["num_ctx"] = 16384 },
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = Prompt.SystemMessage },
                new JsonObject { ["role"] = "user", ["content"] = EmotePrompt.User(text) },
            },
        };

        var watch = Stopwatch.StartNew();
        using var response = await http.PostAsJsonAsync("api/chat", request, cancellation).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ollama answered {(int)response.StatusCode}: {body}");
        }

        var content = JsonNode.Parse(body)?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
        return new AiAnswer(EmotePrompt.ParseAnswer(content, commands), watch.Elapsed);
    }
}
