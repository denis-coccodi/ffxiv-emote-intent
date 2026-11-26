using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>A phrase file with "@ /a /b" is a family: its phrases play whichever member the character has.</summary>
public class PhraseFamilyTests
{
    private static readonly PhraseBank Bank = PhraseBank.Parse(
    [
        ("jumpforjoy", "# Jump for Joy 1-5\n@ /jumpforjoy1 /jumpforjoy2 /jumpforjoy3\njump for joy\nleap for joy\n"),
        ("hug", "give you a hug\n"),
    ]);

    private static readonly PhraseMatcher Matcher = new(Bank);

    [Fact]
    public void AFamilyFileListsItsMembers()
    {
        Assert.Equal(["/jumpforjoy1", "/jumpforjoy2", "/jumpforjoy3"], Bank.Commands["/jumpforjoy"]);
        Assert.Equal(["jump for joy", "leap for joy"], Bank.Phrases["/jumpforjoy"]);
    }

    [Fact]
    public void APlainFileIsItsOwnCommand() => Assert.Equal(["/hug"], Bank.Commands["/hug"]);

    [Fact]
    public void PlaysTheFirstMemberTheCharacterHas() =>
        Assert.Equal("/jumpforjoy2", Assert.Single(Matcher.Rank("jump for joy", ["/jumpforjoy3", "/jumpforjoy2"])).Command);

    [Fact]
    public void OffersNothingWhenNoMemberIsUnlocked() => Assert.Empty(Matcher.Rank("jump for joy", ["/hug"]));
}
