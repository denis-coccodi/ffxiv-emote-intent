using System.Text.Json;
using EmoteIntent;
using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

// Usage: dotnet run --project tools/EmoteExport [-- <game folder> [<output file>]]
// The game folder defaults to the one XIVLauncher installed (launcherConfigV3.json's GamePath).
var gamePath = args.Length > 0 ? args[0] : GamePathFromXivLauncher();
var output = args.Length > 1 ? args[1] : Path.Combine(RepoRoot(), "EmoteIntent.Core", "Data", "emotes.json");

var game = new GameData(Path.Combine(gamePath, "game", "sqpack"), new LuminaOptions { DefaultExcelLanguage = Language.English });
var sheet = game.GetExcelSheet<Emote>() ?? throw new InvalidOperationException($"No Emote sheet in {gamePath}");
// EmoteRows is the plugin's own file, compiled in here too: both read emotes the same way.
var emotes = EmoteRows.Playable(sheet).Select(EmoteRows.ToInfo).ToList();

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
var json = JsonSerializer.Serialize(emotes, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
File.WriteAllText(output, json.ReplaceLineEndings("\n") + "\n");
Console.WriteLine($"Wrote {emotes.Count} emotes to {output}");

static string GamePathFromXivLauncher()
{
    var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "launcherConfigV3.json");
    using var document = JsonDocument.Parse(File.ReadAllText(config));
    return document.RootElement.GetProperty("GamePath").GetString()
        ?? throw new InvalidOperationException($"No GamePath in {config}: pass the game folder as the first argument");
}

static string RepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EmoteIntent.slnx")))
    {
        dir = dir.Parent;
    }

    return dir?.FullName ?? throw new InvalidOperationException("EmoteIntent.slnx not found: pass the output file as the second argument");
}
