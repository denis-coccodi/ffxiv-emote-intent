using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

public class EmoteDescriptionTests
{
    [Theory]
    [InlineData(
        "USAGE:  /bow [subcommand]  Perform a bow.  >>Subcommands:  motion    Perform motion only.  Both text and motion will be displayed when no subcommand is specified.",
        "Perform a bow.")]
    [InlineData(
        "USAGE:  /gcsalute [subcommand]  Perform an official Order of the Twin Adder salute.    >>Subcommands:  motion",
        "Perform an official Order of the Twin Adder salute.")]
    [InlineData("USAGE: /visor Open/close helm visor.", "Open/close helm visor.")]
    [InlineData(
        "ALIASES: /fistbump, /brofist USAGE: /fistbump [subcommand] Attempt a fistbump. >>Subcommands: motion",
        "Attempt a fistbump.")]
    [InlineData(
        "ALIASES: /sit, /lounge USAGE: /sit Assume a sitting position. Certain emotes have special sitting equivalents.",
        "Assume a sitting position. Certain emotes have special sitting equivalents.")]
    [InlineData("  Hug your target.  ", "Hug your target.")]
    public void KeepsTheSentenceThatSaysWhatTheEmoteDoes(string help, string description) =>
        Assert.Equal(description, EmoteDescription.Clean(help));
}
