# Claude Code PostToolUse hook (Edit|Write): formats the C# file Claude just wrote with
# `dotnet format whitespace`, so the format check in the `test` workflow stays green.
# Style and analyzer rules aren't fixed here: the build reports those (csharp-best-practices skill).
# Windows PowerShell 5.1 compatible. Exit 0 = fine; a failure is reported but doesn't block.
$ErrorActionPreference = 'Stop'

$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$file = $payload.tool_input.file_path
if (-not $file -or $file -notmatch '\.cs$' -or -not (Test-Path -LiteralPath $file)) { exit 0 }

# The solution the file belongs to (a worktree has its own copy).
$root = Split-Path -Parent (Resolve-Path -LiteralPath $file).Path
while ($root -and -not (Test-Path -LiteralPath (Join-Path $root 'EmoteIntent.slnx'))) {
    $root = Split-Path -Parent $root
}
if (-not $root) { exit 0 }

Push-Location -LiteralPath $root
try {
    $relative = Resolve-Path -LiteralPath $file -Relative
    $output = & dotnet format whitespace EmoteIntent.slnx --include $relative 2>&1
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine("dotnet format whitespace failed on ${relative}: $output")
        exit 1
    }
}
finally {
    Pop-Location
}
