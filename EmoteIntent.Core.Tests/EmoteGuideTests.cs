using System.Text.Json;
using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Data/emote-guide.txt describes every emote of the game, and only those.</summary>
public class EmoteGuideTests
{
    private static readonly EmoteGuide Guide = EmoteGuide.LoadEmbedded();
    private static readonly HashSet<string> Commands = JsonSerializer.Deserialize<List<EmoteInfo>>(
            File.ReadAllText(Path.Combine(RepoRulesTests.RepoRoot(), "EmoteIntent.Core", "Data", "emotes.json")),
            JsonSerializerOptions.Web)!
        .Select(emote => emote.Command)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void EveryEmoteHasAnEntry() =>
        Assert.DoesNotContain(Commands, command => !Guide.Entries.ContainsKey(command));

    [Fact]
    public void EveryEntryIsARealEmote() =>
        Assert.DoesNotContain(Guide.Entries.Keys, command => !Commands.Contains(command));

    [Fact]
    public void RejectsALineWithoutThreeParts() =>
        Assert.Throws<InvalidDataException>(() => EmoteGuide.Parse("/bow | Bows."));
}
