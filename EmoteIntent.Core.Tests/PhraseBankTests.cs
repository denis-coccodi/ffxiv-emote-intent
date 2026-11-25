using System.Text.Json;
using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Checks the shipped phrase bank against the game's emote list.</summary>
public class PhraseBankTests
{
    private const int MinPhrasesPerEmote = 30;

    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();

    [Fact]
    public void LoadsTheEmbeddedPhrases() => Assert.True(Bank.Phrases.Count >= 30, $"Only {Bank.Phrases.Count} emotes in the bank");

    [Fact]
    public void EveryFileIsAnEmoteOfTheGame()
    {
        var path = Path.Combine(RepoRulesTests.RepoRoot(), "EmoteIntent.Core", "Data", "emotes.json");
        var commands = JsonSerializer.Deserialize<List<EmoteInfo>>(File.ReadAllText(path), JsonSerializerOptions.Web)!
            .Select(emote => emote.Command)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(Bank.Phrases.Keys, command => Assert.Contains(command, commands));
    }

    [Fact]
    public void EveryEmoteHasEnoughPhrases() =>
        Assert.All(Bank.Phrases, entry => Assert.True(
            entry.Value.Count >= MinPhrasesPerEmote, $"{entry.Key} has {entry.Value.Count} phrases, at least {MinPhrasesPerEmote} expected"));

    [Fact]
    public void NoPhraseBelongsToTwoEmotes()
    {
        var clashes = Bank.Phrases
            .SelectMany(entry => entry.Value.Select(phrase => (Phrase: TextNormalizer.Compact(phrase), Command: entry.Key)))
            .GroupBy(item => item.Phrase, StringComparer.Ordinal)
            .Where(group => group.Select(item => item.Command).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(group => $"\"{group.Key}\": {string.Join(", ", group.Select(item => item.Command).Distinct())}")
            .ToList();

        Assert.True(clashes.Count == 0, "Phrases in more than one emote:\n" + string.Join('\n', clashes));
    }
}
