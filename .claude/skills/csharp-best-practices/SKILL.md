---
name: csharp-best-practices
description: How C# is written in this repo - .NET 10 / C# 14 style, nullable and analyzers as build errors, banned APIs (BannedSymbols.txt), the Core vs plugin split, xUnit tests, file size limits, and the hooks that format and guard files. Use before writing or changing any .cs, .csproj or Directory.Build.props file, when adding a package or a test, and when reviewing a change.
---

# C# in this repo

.NET 10, C# 14, `Nullable` and `ImplicitUsings` on everywhere (`Directory.Build.props`). **The build is the check**: warnings are errors, .NET's code-quality rules run at `latest-recommended`, and `.editorconfig`'s style rules are enforced in the build (`EnforceCodeStyleInBuild`). Don't silence a rule to get green: fix the code, or explain in the PR why a `#pragma warning disable` with a comment is right.

## Where code goes

- **`EmoteIntent.Core`**: everything that doesn't need the game: the emote model, matching, the phrase bank, configuration logic. No reference to Dalamud, Lumina or FFXIVClientStructs, ever (`RepoRulesTests.CoreDoesNotDependOnTheGame` fails otherwise). Every public member has an XML doc comment (`CS1591` is an error in Core).
- **`EmoteIntent`**: the Dalamud plugin, kept thin: services, commands, windows, reading game data into Core's types (`EmoteCatalog`), and calling the game (`ChatCommand`). Logic that can be tested without the game moves to Core.
- **`EmoteIntent.Core.Tests`**: xUnit tests for Core.
- A new project is added to `EmoteIntent.slnx` and inherits `Directory.Build.props`; never turn its rules off locally.

## Style

- File-scoped namespaces matching the folder; one top-level type per file, named like the file.
- `sealed` classes by default; `record` (or `readonly record struct` when small) for data; `static` classes for pure functions.
- Primary constructors for dependencies (`internal sealed class EmoteCatalog(IDataManager dataManager, ...)`); fields `readonly`.
- `var` when the type is obvious from the right-hand side; collection expressions (`[]`, `[a, b]`); pattern matching (`is null`, `is "bin" or "obj"`) over chains of `==`.
- Braces on every `if`/`for`/`while`, even single lines (`csharp_prefer_braces`).
- Expose read-only shapes (`IReadOnlyList<T>`, `IEnumerable<T>`), never a mutable `List<T>` you keep using.
- No `!` (null-forgiving) except the `= null!` initialiser of a `[PluginService]` property; handle the null instead.
- Strings compared with an explicit `StringComparison` (`Ordinal`, or `OrdinalIgnoreCase` for commands and names); `ToLowerInvariant`, never culture-dependent casing.
- Comments say why, not what; XML docs say what a member does for its caller.

## Async and threads

- The game runs plugin code on its **main thread** (framework updates, command handlers, UI draw). Never block it: `Thread.Sleep`, `Task.Wait()`, `.Result` and `.GetAwaiter().GetResult()` are banned (`BannedSymbols.txt`).
- Long work (loading the phrase bank, embeddings, an AI call) runs off the main thread with `async`/`await` and `Task.Run`; anything that touches the game comes back through `IFramework.RunOnFrameworkThread` (dalamud-plugin skill).
- Pass a `CancellationToken` through async work and cancel it in `Dispose`, so unloading the plugin stops it.
- No `async void` except event handlers.

## Banned APIs

`BannedSymbols.txt` lists APIs that fail the build (RS0030), each with what to use instead: `Console`, blocking waits, `DateTime.Now` (use `UtcNow`), `Environment.Exit`, `GC.Collect`, and `HttpClient` (no network without the player opting in). Add to it when a review finds something that must never come back, with the reason after the `;`.

## Packages

- Add a package only when the framework can't do it; prefer what .NET or Dalamud already ships (Dalamud provides Newtonsoft.Json, Serilog via `IPluginLog`, ImGui bindings: don't add your own copies).
- The plugin project restores with a lock file (`packages.lock.json`, required by Dalamud's build): after changing a `PackageReference`, run `dotnet restore --force-evaluate` and commit the lock file. Never edit it by hand (the protect-files hook refuses it).
- Analyzer-only packages get `PrivateAssets="all"`. Dependabot proposes updates weekly.

## Tests

- xUnit; test names say the behaviour: `MatchesNamesWithSpacesAndAliases`, `ReturnsNullWhenNothingIsNamedExactly`. `[Theory]` + `[InlineData]` for the same rule over several inputs.
- Arrange data in the test or in `private static readonly` fields, not shared mutable state.
- A bug fix comes with a test that fails without it. New Core behaviour comes with tests; plugin glue that needs the game is tried in the game instead, and the PR says so.
- `RepoRulesTests` holds the structural rules (Core without game references, file sizes).

## Size limits

A `.cs` file stays at or under **300 lines** (`RepoRulesTests.SourceFilesStaySmall`). Before reaching it, split by responsibility (a new class or a partial for a generated part), not by cutting a method in half. Methods stay short enough to read in one screen.

## Formatting and hooks

- `dotnet format --verify-no-changes` runs in the `test` workflow and the `pre-push` git hook.
- In Claude Code, the `format-csharp.ps1` hook runs `dotnet format whitespace` on every `.cs` file written, and `protect-files.ps1` refuses edits to `bin/`, `obj/`, `packages.lock.json` and `LICENSE` (`.claude/settings.json`). Both are Windows PowerShell 5.1 scripts in `.claude/hooks/`.
- The `pre-push` git hook runs build, tests and the format check before a push; the `commit-msg` hook checks Conventional Commits. The first `dotnet build` points git at `.githooks`.

## Before pushing

```
dotnet build -c Release
dotnet test -c Release --no-build
dotnet format --verify-no-changes
```

The `pre-push` hook runs these too; if it fails, fix the cause rather than skipping it with `--no-verify`.
