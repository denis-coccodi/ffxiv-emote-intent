namespace EmoteIntent.Core;

/// <summary>Turns the game's emote help text into the sentence that says what the emote does.</summary>
public static class EmoteDescription
{
    private const string Usage = "USAGE:";
    private const string Subcommands = ">>";

    /// <summary>
    /// "USAGE:  /bow [subcommand]  Perform a bow.  >>Subcommands: motion …" → "Perform a bow.": the usage line and the
    /// subcommand list go, spaces collapse. Text in another shape comes back trimmed.
    /// </summary>
    public static string Clean(string help)
    {
        var text = help.Trim();
        var subcommands = text.IndexOf(Subcommands, StringComparison.Ordinal);
        if (subcommands >= 0)
        {
            text = text[..subcommands];
        }

        // "ALIASES: /a, /b USAGE: /a [subcommand] …": the alias list goes too.
        var usage = text.IndexOf(Usage, StringComparison.Ordinal);
        if (usage >= 0)
        {
            var afterUsage = text[(usage + Usage.Length)..].TrimStart();
            var bracket = afterUsage.IndexOf(']', StringComparison.Ordinal);
            var space = afterUsage.IndexOf(' ', StringComparison.Ordinal);
            var cut = bracket >= 0 ? bracket + 1 : space >= 0 ? space : afterUsage.Length;
            text = afterUsage[cut..];
        }

        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
