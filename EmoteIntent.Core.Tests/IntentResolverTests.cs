using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>What players type → what /feel does, with the shipped phrase bank and every bank emote unlocked.</summary>
public class IntentResolverTests
{
    private static readonly PhraseBank Bank = PhraseBank.LoadEmbedded();
    private static readonly IntentResolver Resolver = new(new PhraseMatcher(Bank));
    private static readonly List<EmoteInfo> AllUnlocked = Bank.Commands.Values.SelectMany(commands => commands).Distinct()
        .Select((command, index) => new EmoteInfo((uint)index + 1, command.TrimStart('/'), command, [], "General"))
        .ToList();

    [Theory]
    [InlineData("so happy to see you", "/greet")]
    [InlineData("Hello everyone!", "/greet")]
    [InlineData("thank you so much for the help", "/bow")]
    [InlineData("lmao that's hilarious", "/laugh")]
    [InlineData("calm down guys", "/soothe")]
    [InlineData("good night all", "/goodbye")]
    [InlineData("yay we cleared it!", "/joy")]
    [InlineData("o7", "/salute")]
    [InlineData("<3", "/dote")]
    [InlineData("rip", "/respect")]
    [InlineData("nice job", "/thumbsup")]
    [InlineData("will you marry me?", "/kneel")]
    [InlineData("love you", "/blowkiss")]
    [InlineData("let's go team", "/rally")]
    [InlineData("welcome to the FC", "/welcome")]
    [InlineData("hand on my heart", "/handtoheart")]
    [InlineData("congrats on your clear!", "/congratulate")]
    [InlineData("give me a big hug", "/hug")]
    [InlineData("good boy", "/stroke")]
    [InlineData("cheers!", "/toast")]
    [InlineData("happy halloween everyone", "/allsaintscharm")]
    [InlineData("here's a tip for you", "/gratuity")]
    [InlineData("omg", "/wow")]
    [InlineData("come at me", "/battlestance")]
    [InlineData("never let me go", "/embrace")]
    [InlineData("paint the town red", "/paintred")]
    [InlineData("i'm all ears", "/earwiggle")]
    [InlineData("it was nothing", "/humbletriumph")]
    [InlineData("brofist", "/fistbump")]
    [InlineData("ta da!", "/magictrick")]
    [InlineData("lol", "/chuckle")]
    [InlineData("idk", "/shrug")]
    [InlineData("i don't know what to do", "/shrug")]
    [InlineData("nope", "/no")]
    [InlineData("zzz", "/doze")]
    [InlineData("i'm hungry", "/eatriceball")]
    [InlineData("boo!", "/frighten")]
    [InlineData("selfie!", "/visage")]
    [InlineData("follow me", "/beckon")]
    [InlineData("that's so sad", "/cry")]
    [InlineData("i'm confused", "/huh")]
    [InlineData("i'm so angry right now", "/furious")]
    [InlineData("you're the best", "/thumbsup")]
    [InlineData("i'm so sorry", "/grovel")]
    [InlineData("high five!", "/highfive")]
    [InlineData("smile", "/smile")]
    [InlineData("i'm cold", "/shiver")]
    [InlineData("ew gross", "/upset")]
    [InlineData("eye roll", "/annoyed")]
    [InlineData("kiss me", "/puckerup")]
    [InlineData("are you okay?", "/worried")]
    [InlineData("drop and give me 20", "/pushups")]
    [InlineData("take a seat", "/lounge")]
    [InlineData("kupo!", "/mogdance")]
    [InlineData("i'm so lonely", "/sad")]
    [InlineData("evil laugh", "/cackle")]
    [InlineData("flowers for you", "/bouquet")]
    [InlineData("thank you!", "/bow")]
    [InlineData("i'm sorry", "/easternbow")]
    [InlineData("what a view", "/amazed")]
    [InlineData("superhero landing!", "/adventoflight")]
    [InlineData("did we win?", "/victoryreveal")]
    [InlineData("i'm so drunk", "/dazed")]
    [InlineData("i'll watch over you", "/guard")]
    [InlineData("what are my orders", "/attend")]
    [InlineData("shaking with rage", "/rage")]
    [InlineData("eat bread", "/breakfast")]
    [InlineData("cast a spell", "/allsaintscharm")]
    [InlineData("sing a song", "/hum")]
    [InlineData("aloha!", "/flamedance")]
    [InlineData("jojo pose", "/poseoftheunbound")]
    [InlineData("bunny hop", "/lophop")]
    [InlineData("disco time", "/popotostep")]
    [InlineData("kung fu", "/crimsonlotus")]
    [InlineData("dab on them", "/rangerpose2r")]
    [InlineData("it's morphin time", "/rangerpose1r")]
    [InlineData("check this out", "/showright")]
    [InlineData("raise my sword to the sky", "/divinearm")]
    [InlineData("riverdance!", "/stepdance")]
    [InlineData("rain dance", "/harvestdance")]
    [InlineData("praise the sun", "/sundance")]
    [InlineData("do the mambo", "/mandervillemambo")]
    [InlineData("sexy dance", "/beesknees")]
    [InlineData("levitate with a glowing sword", "/simulationm")]
    [InlineData("plunge my sword into the ground", "/sundering")]
    [InlineData("water the flowers", "/water")]
    [InlineData("on my smartphone", "/tomestone")]
    public void PlaysTheEmoteThatClearlyFits(string text, string command)
    {
        var result = Resolver.Resolve(text, AllUnlocked);

        var play = Assert.IsType<IntentResult.Play>(result);
        Assert.Equal(command, play.Emote.Command);
    }

    [Fact]
    public void PlaysAnEmoteNamedOutright() =>
        Assert.Equal("/dance", Assert.IsType<IntentResult.Play>(Resolver.Resolve("dance", AllUnlocked)).Emote.Command);

    [Fact]
    public void SuggestsWhenSeveralEmotesMightFit()
    {
        var result = Resolver.Resolve("so glad you are here", AllUnlocked);

        var suggest = Assert.IsType<IntentResult.Suggest>(result);
        Assert.InRange(suggest.Emotes.Count, 2, IntentResolver.SuggestCount);
    }

    [Theory]
    [InlineData("qwertyuiop zxcvbnm")]
    [InlineData("")]
    public void FindsNothingWhenNothingFits(string text) =>
        Assert.IsType<IntentResult.NoMatch>(Resolver.Resolve(text, AllUnlocked));

    [Fact]
    public void OnlyOffersUnlockedEmotes()
    {
        var withoutGreet = AllUnlocked.Where(emote => emote.Command != "/greet").ToList();

        var result = Resolver.Resolve("so happy to see you", withoutGreet);

        Assert.NotEmpty(Offered(result));
        Assert.DoesNotContain(Offered(result), emote => emote.Command == "/greet");
    }

    private static IEnumerable<EmoteInfo> Offered(IntentResult result) => result switch
    {
        IntentResult.Play play => [play.Emote],
        IntentResult.Suggest suggest => suggest.Emotes,
        _ => [],
    };
}
