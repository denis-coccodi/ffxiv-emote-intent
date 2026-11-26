// Compares the phrase bank with a model in Ollama on everyday sentences (cases.txt): how often each one's first
// choice is right, how often a right emote is among its first three, and how fast the model answers.
//
//   dotnet run --project tools/AiEval -c Release -- qwen3:4b [granite4:3b …] [--cases file] [--url http://127.0.0.1:11434]
//
// Every emote counts as unlocked. Answers that are wrong are listed, so they can be judged by eye.
using EmoteIntent.Core;
using EmoteIntent.Core.Ai;

var models = args.TakeWhile(arg => !arg.StartsWith("--", StringComparison.Ordinal)).ToList();
var casesPath = Option("--cases") ?? Path.Combine(AppContext.BaseDirectory, "cases.txt");
var address = new Uri(Option("--url") ?? OllamaClient.DefaultAddress.ToString());

var bank = PhraseBank.LoadEmbedded();
var matcher = new PhraseMatcher(bank);
var resolver = new IntentResolver(matcher);
var guide = EmoteGuide.LoadEmbedded();
var commands = guide.Entries.Keys.ToList();
var unlocked = commands.Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General")).ToList();
var cases = File.ReadAllLines(casesPath)
    .Where(line => line.Length > 0 && !line.StartsWith('#'))
    .Select(line => line.Split('|'))
    .Select(parts => (Text: parts[0].Trim(), Right: parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase)))
    .ToList();
var unknown = cases.SelectMany(item => item.Right).Where(command => !guide.Entries.ContainsKey(command)).Distinct().ToList();
if (unknown.Count > 0)
{
    throw new InvalidDataException($"Not emotes, in {casesPath}: {string.Join(' ', unknown)}");
}

Console.WriteLine($"{cases.Count} sentences, {commands.Count} emotes.");
Report("phrase bank", cases.Select(item => (item.Text, item.Right, BankAnswer(item.Text), TimeSpan.Zero)).ToList());

using var http = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromMinutes(2) };
var prompt = new EmotePrompt(guide);
foreach (var model in models)
{
    var client = new OllamaClient(http, prompt, model);
    // The first request loads the model and reads the whole guide; later ones reuse it, as in the game.
    await client.PickAsync("hello", commands, CancellationToken.None);
    var results = new List<(string, HashSet<string>, IReadOnlyList<string>, TimeSpan)>();
    foreach (var (text, right) in cases)
    {
        var answer = await client.PickAsync(text, commands, CancellationToken.None);
        results.Add((text, right, answer.Commands, answer.Took));
    }

    Report(model, results);
}

// The phrase bank's answer as /feel shows it: the emote it plays, then the alternatives; or what it suggests.
IReadOnlyList<string> BankAnswer(string text) => resolver.Resolve(text, unlocked) switch
{
    IntentResult.Play play => [play.Emote.Command, .. play.Alternatives.Select(emote => emote.Command)],
    IntentResult.Suggest suggest => suggest.Emotes.Select(emote => emote.Command).ToList(),
    _ => [],
};

void Report(string name, List<(string Text, HashSet<string> Right, IReadOnlyList<string> Answer, TimeSpan Took)> results)
{
    var first = results.Count(result => result.Answer.Count > 0 && result.Right.Contains(result.Answer[0]));
    var three = results.Count(result => result.Answer.Take(3).Any(result.Right.Contains));
    var none = results.Count(result => result.Answer.Count == 0);
    Console.WriteLine();
    Console.WriteLine($"== {name}: first choice right {first}/{results.Count}, a right one in the first three {three}/{results.Count}, nothing {none}");
    var times = results.Select(result => result.Took.TotalMilliseconds).Order().ToList();
    if (times[^1] > 0)
    {
        Console.WriteLine($"   answer time: median {times[times.Count / 2]:0} ms, slowest {times[^1]:0} ms");
    }

    foreach (var (text, right, answer, _) in results.Where(result => result.Answer.Count == 0 || !result.Right.Contains(result.Answer[0])))
    {
        Console.WriteLine($"   ✗ {text} → {(answer.Count == 0 ? "nothing" : string.Join(' ', answer))}");
    }
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
