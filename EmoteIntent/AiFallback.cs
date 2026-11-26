using Dalamud.Plugin.Services;
using EmoteIntent.Core;
using EmoteIntent.Core.Ai;

namespace EmoteIntent;

/// <summary>
/// The opt-in AI: when the phrase bank isn't sure, a model running in Ollama on this PC says what the text means in
/// plain words, and the phrase bank turns those into emotes. Runs in the background; the answer is shown on the
/// game's thread. If Ollama can't answer, the phrase bank's own result is shown.
/// </summary>
internal sealed class AiFallback(
    Configuration configuration, IntentResolver resolver, IFramework framework, IChatGui chatGui, IPluginLog log)
    : IDisposable
{
    // A model loads in a few seconds on first use; past this, the phrase bank's result is shown instead.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly EmotePrompt prompt = new(EmoteGuide.LoadEmbedded());
    private OllamaClient? client;
    private CancellationTokenSource? pending;
    private bool warned;
    private volatile bool disposed;

    /// <summary>Whether to ask the model about this result: switched on, and the phrase bank didn't find a clear match.</summary>
    public bool Wanted(IntentResult result) => configuration.UseAi && result is IntentResult.Suggest or IntentResult.NoMatch;

    /// <summary>
    /// Asks the model about <paramref name="text"/>, then calls <paramref name="show"/> on the game's thread with the
    /// emotes its meaning fits among <paramref name="emotes"/>, or with <paramref name="fallback"/> if it can't
    /// answer. A newer request cancels this one: its answer is never shown.
    /// </summary>
    public void Ask(string text, IReadOnlyList<EmoteInfo> emotes, IntentResult fallback, Action<IntentResult> show)
    {
        // The older request's source is left to its own timer: its task may still be reading the token.
        pending?.Cancel();
        var mine = pending = new CancellationTokenSource(Timeout);
        var token = mine.Token;
        var model = Client();
        _ = Task.Run(async () =>
        {
            var result = fallback;
            try
            {
                var (meanings, took) = await model.MeaningsAsync(text, token).ConfigureAwait(false);
                log.Debug("{Model} read \"{Text}\" as {Meanings} in {Took} ms", model.Model, text, meanings, took.TotalMilliseconds);
                var understood = resolver.ResolveMeanings(meanings, emotes);
                if (understood is not IntentResult.NoMatch)
                {
                    result = understood;
                }

                warned = false;
            }
            catch (OperationCanceledException) when (Stale(mine))
            {
                return;
            }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException or ObjectDisposedException
                or System.Text.Json.JsonException)
            {
                log.Warning(error, "Ollama couldn't answer");
                if (!warned)
                {
                    warned = true;
                    await framework.RunOnFrameworkThread(() => chatGui.Print(
                        $"The AI ({model.Model} in Ollama) didn't answer: is Ollama running? Showing the phrase bank's matches."))
                        .ConfigureAwait(false);
                }
            }

            // Shown on the game's thread, unless a newer /feel replaced it or the plugin is unloading.
            await framework.RunOnFrameworkThread(() =>
            {
                if (!Stale(mine))
                {
                    show(result);
                }
            }).ConfigureAwait(false);
        });
    }

    /// <summary>Whether Ollama is running with the chosen model, for the settings window.</summary>
    public Task<OllamaStatus> CheckAsync() => Client().CheckAsync(CancellationToken.None);

    public void Dispose()
    {
        disposed = true;
        pending?.Cancel();
        pending?.Dispose();
        client?.Dispose();
    }

    // A newer request replaced this one, or the plugin is unloading: its answer isn't wanted.
    private bool Stale(CancellationTokenSource request) => disposed || !ReferenceEquals(pending, request);

    // One client per model: a new one when the player changes the model.
    private OllamaClient Client()
    {
        if (client is null || client.Model != configuration.AiModel)
        {
            client?.Dispose();
            client = new OllamaClient(prompt, configuration.AiModel, Timeout);
        }

        return client;
    }
}
