using System.Globalization;
using System.Text;
using EmoteIntent.Core;

/// <summary>
/// A sheet for a person to judge the setups without knowing which picked what: every sentence where their first
/// choices differ, with those emotes shuffled and unlabelled, to tick the ones that fit. The setups' answers are
/// saved beside it (sheet + ".answers.tsv"), so the ticked sheet can be scored later without running the models.
/// </summary>
internal static class BlindSheet
{
    private const char Tab = '\t';

    /// <summary>Writes the sheet and its answers file.</summary>
    public static void Write(string path, EmoteGuide guide, IReadOnlyList<Run> runs)
    {
        var random = new Random(26);
        var sheet = new StringBuilder();
        sheet.AppendLine("# For each sentence, tick every emote that fits what someone typing it after /feel would mean:");
        sheet.AppendLine("# put an x in its brackets, like [x]. Leave the others empty. The order is shuffled and nothing");
        sheet.AppendLine("# says which setup picked which emote. Sentences where every setup picked the same are left out.");
        var number = 0;
        foreach (var text in runs[0].Answers.Keys)
        {
            var firsts = runs.Select(run => First(run, text)).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (firsts.Count < 2 && runs.All(run => First(run, text) is not null))
            {
                continue;
            }

            sheet.AppendLine();
            sheet.AppendLine(CultureInfo.InvariantCulture, $"{++number}. {text}");
            foreach (var command in firsts.OrderBy(_ => random.Next()))
            {
                var shows = guide.Entries.TryGetValue(command, out var entry) ? entry.Shows : "";
                sheet.AppendLine(CultureInfo.InvariantCulture, $"   [ ] {command}  {shows}");
            }
        }

        File.WriteAllText(path, sheet.ToString());
        File.WriteAllLines(AnswersPath(path), runs.SelectMany(run => run.Answers.Select(answer =>
            $"{run.Name}{Tab}{answer.Key}{Tab}{string.Join(' ', answer.Value)}")));
        Console.WriteLine($"\n{number} sentences to judge in {path}");
    }

    /// <summary>Scores each setup's first choices against the ticks in a judged sheet.</summary>
    public static void Score(string path)
    {
        var ticked = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        HashSet<string>? current = null;
        foreach (var line in File.ReadAllLines(path).Where(line => line.Length > 0 && !line.StartsWith('#')))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('['))
            {
                current = ticked[trimmed[(trimmed.IndexOf(". ", StringComparison.Ordinal) + 2)..]] = new(StringComparer.OrdinalIgnoreCase);
            }
            else if (current is not null && trimmed.Length > 4 && trimmed[1] is not ' ' and not ']')
            {
                current.Add(trimmed[3..].Trim().Split(' ')[0]);
            }
        }

        var answers = File.ReadAllLines(AnswersPath(path)).Select(line => line.Split(Tab)).ToList();
        Console.WriteLine($"{ticked.Count} judged sentences, first choice ticked as fitting:");
        foreach (var setup in answers.GroupBy(parts => parts[0]))
        {
            var judged = setup.Where(parts => ticked.ContainsKey(parts[1])).ToList();
            var right = judged.Count(parts => parts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries) is [var first, ..] && ticked[parts[1]].Contains(first));
            Console.WriteLine($"   {right,3}/{judged.Count}  {setup.Key}");
        }
    }

    private static string? First(Run run, string text) =>
        run.Answers.TryGetValue(text, out var answer) && answer.Count > 0 ? answer[0] : null;

    private static string AnswersPath(string path) => path + ".answers.tsv";
}

/// <summary>One setup's answers, by sentence.</summary>
internal sealed record Run(string Name, IReadOnlyDictionary<string, IReadOnlyList<string>> Answers);
