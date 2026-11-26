using System.Text.Json;
using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>Checks EmoteIntent.Core/Data/emotes.json, the list the phrase bank is written against.</summary>
public class EmoteDataTests
{
    private static readonly List<EmoteInfo> Emotes = Load();

    [Fact]
    public void HoldsEveryEmoteOfTheGame() => Assert.True(Emotes.Count >= 290, $"Only {Emotes.Count} emotes: rerun tools/EmoteExport");

    [Fact]
    public void EveryEmoteHasANameAndACommand() =>
        Assert.All(Emotes, emote =>
        {
            Assert.False(string.IsNullOrWhiteSpace(emote.Name));
            Assert.StartsWith("/", emote.Command, StringComparison.Ordinal);
            Assert.All(emote.Aliases, alias => Assert.StartsWith("/", alias, StringComparison.Ordinal));
        });

    // Commands may be shared: /gcsalute plays the Storm, Serpent or Flame Salute, whichever matches the
    // player's Grand Company. Ids and names are what tell emotes apart.
    [Fact]
    public void IdsAndNamesAreUnique()
    {
        Assert.Equal(Emotes.Count, Emotes.Select(emote => emote.Id).Distinct().Count());
        Assert.Equal(Emotes.Count, Emotes.Select(emote => emote.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    private static List<EmoteInfo> Load()
    {
        var path = Path.Combine(RepoRulesTests.RepoRoot(), "EmoteIntent.Core", "Data", "emotes.json");
        return JsonSerializer.Deserialize<List<EmoteInfo>>(File.ReadAllText(path), JsonSerializerOptions.Web)
            ?? throw new InvalidDataException($"{path} holds no emotes");
    }
}
