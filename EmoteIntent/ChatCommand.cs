using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace EmoteIntent;

/// <summary>Runs a chat command exactly as if the player had typed it into the chat box.</summary>
internal static unsafe class ChatCommand
{
    /// <summary>
    /// Runs <paramref name="command"/> (e.g. "/hug") through the game's chat box. Call it on the game's main
    /// thread (a command handler already runs there), and only once per player action: Dalamud forbids automation.
    /// </summary>
    public static void Run(string command)
    {
        if (!command.StartsWith('/') || command.Contains('\n') || command.Contains('\r'))
        {
            throw new ArgumentException($"Not a single chat command: \"{command}\"", nameof(command));
        }

        var text = Utf8String.FromString(command);
        try
        {
            UIModule.Instance()->ProcessChatBoxEntry(text);
        }
        finally
        {
            text->Dtor(true);
        }
    }
}
