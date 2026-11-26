using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace EmoteIntent.Core.Ai;

/// <summary>Whether Ollama can answer: running, and with the model downloaded.</summary>
public enum OllamaStatus
{
    /// <summary>Running, with the model.</summary>
    Ready,

    /// <summary>Nothing answers at Ollama's address.</summary>
    NotRunning,

    /// <summary>Running, but the model isn't downloaded ("ollama pull gemma3:4b").</summary>
    ModelMissing,
}

/// <summary>A model's answer: the emotes it chose, best first (possibly none), and how long it took.</summary>
public sealed record AiAnswer(IReadOnlyList<string> Commands, TimeSpan Took);

/// <summary>
/// Asks a model running in Ollama on the player's own PC (http://127.0.0.1:11434 by default) to choose emotes.
/// Free and private: nothing leaves the PC. Only used when the player switched it on. The one place that talks to
/// the network, which is why it may use HttpClient (banned everywhere else, see BannedSymbols.txt).
/// </summary>
#pragma warning disable RS0030 // The opted-in AI client is the only code allowed to make network calls.
public sealed class OllamaClient : IDisposable
{
    /// <summary>Where Ollama listens unless told otherwise.</summary>
    public static readonly Uri DefaultAddress = new("http://127.0.0.1:11434/");

    private readonly HttpClient http;
    private readonly bool ownsHttp;

    /// <summary>A client using <paramref name="http"/> (its BaseAddress is Ollama's address).</summary>
    public OllamaClient(HttpClient http, EmotePrompt prompt, string model)
    {
        this.http = http;
        Prompt = prompt;
        Model = model;
    }

    /// <summary>A client of its own for Ollama on this PC (<see cref="DefaultAddress"/>), for the plugin.</summary>
    public OllamaClient(EmotePrompt prompt, string model, TimeSpan timeout)
        : this(new HttpClient { BaseAddress = DefaultAddress, Timeout = timeout }, prompt, model) => ownsHttp = true;

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
        var (content, took) = await ChatAsync(
            Prompt.SystemMessage, EmotePrompt.User(text), EmotePrompt.Schema(commands), 16384, cancellation).ConfigureAwait(false);
        return new AiAnswer(EmotePrompt.ParseAnswer(content, commands), took);
    }

    /// <summary>
    /// What <paramref name="text"/> means in a few plain words ("frustrated", "dead"), for the phrase bank to turn into
    /// emotes (<see cref="MeaningPrompt"/>), and how long it took.
    /// </summary>
    public async Task<(IReadOnlyList<string> Meanings, TimeSpan Took)> MeaningsAsync(string text, CancellationToken cancellation)
    {
        var (content, took) = await ChatAsync(
            MeaningPrompt.SystemMessage, MeaningPrompt.User(text), MeaningPrompt.Schema(), 2048, cancellation).ConfigureAwait(false);
        return (MeaningPrompt.ParseAnswer(content), took);
    }

    /// <summary>Whether Ollama is running and has <see cref="Model"/>.</summary>
    public async Task<OllamaStatus> CheckAsync(CancellationToken cancellation)
    {
        try
        {
            var tags = await http.GetStringAsync(new Uri("api/tags", UriKind.Relative), cancellation).ConfigureAwait(false);
            return HasModel(tags, Model) ? OllamaStatus.Ready : OllamaStatus.ModelMissing;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // Refused, timed out, or something else answers on that port.
            return OllamaStatus.NotRunning;
        }
    }

    /// <summary>
    /// Whether Ollama's model list (its /api/tags answer) holds <paramref name="model"/>; "gemma3" means
    /// "gemma3:latest".
    /// </summary>
    public static bool HasModel(string tagsJson, string model)
    {
        var wanted = model.Contains(':', StringComparison.Ordinal) ? model : model + ":latest";
        return JsonNode.Parse(tagsJson)?["models"]?.AsArray()
            .Any(entry => string.Equals(entry?["name"]?.GetValue<string>(), wanted, StringComparison.OrdinalIgnoreCase)) == true;
    }

    /// <summary>Closes the connection the client opened itself; one it was given stays its owner's.</summary>
    public void Dispose()
    {
        if (ownsHttp)
        {
            http.Dispose();
        }
    }

    private async Task<(string Content, TimeSpan Took)> ChatAsync(
        string system, string user, JsonObject schema, int contextLength, CancellationToken cancellation)
    {
        var request = new JsonObject
        {
            ["model"] = Model,
            ["stream"] = false,
            // Thinking models (qwen3…) would think aloud first: slower, and this is a quick choice.
            ["think"] = false,
            // Keep the model and its cached instructions loaded between /feel commands.
            ["keep_alive"] = "30m",
            ["format"] = schema,
            ["options"] = new JsonObject { ["temperature"] = 0, ["num_ctx"] = contextLength },
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = system },
                new JsonObject { ["role"] = "user", ["content"] = user },
            },
        };

        var watch = Stopwatch.StartNew();
        using var response = await http.PostAsJsonAsync("api/chat", request, cancellation).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ollama answered {(int)response.StatusCode}: {body}");
        }

        return (JsonNode.Parse(body)?["message"]?["content"]?.GetValue<string>() ?? string.Empty, watch.Elapsed);
    }
}
#pragma warning restore RS0030
