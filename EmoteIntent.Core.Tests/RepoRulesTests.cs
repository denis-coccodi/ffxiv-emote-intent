using System.Reflection;
using EmoteIntent.Core;

namespace EmoteIntent.Core.Tests;

/// <summary>The repo's structural rules (csharp-best-practices skill), checked like any other test.</summary>
public class RepoRulesTests
{
    /// <summary>Most lines a .cs file may have; split by responsibility before reaching it.</summary>
    private const int MaxLinesPerFile = 300;

    private static readonly string[] GameAssemblies = ["Dalamud", "Lumina", "FFXIVClientStructs", "InteropGenerator"];

    [Fact]
    public void CoreDoesNotDependOnTheGame()
    {
        var references = typeof(EmoteInfo).Assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty);

        Assert.DoesNotContain(references, name => GameAssemblies.Any(game => name.StartsWith(game, StringComparison.Ordinal)));
    }

    [Fact]
    public void SourceFilesStaySmall()
    {
        var tooLong = Directory.EnumerateFiles(RepoRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Select(path => (path, lines: File.ReadLines(path).Count()))
            .Where(file => file.lines > MaxLinesPerFile)
            .Select(file => $"{Path.GetRelativePath(RepoRoot(), file.path)}: {file.lines} lines")
            .ToList();

        Assert.True(tooLong.Count == 0, $"Over {MaxLinesPerFile} lines, split by responsibility:\n{string.Join('\n', tooLong)}");
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "bin" or "obj");

    internal static string RepoRoot()
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EmoteIntent.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("EmoteIntent.slnx not found above the test assembly");
    }
}
