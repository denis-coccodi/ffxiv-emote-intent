# Claude Code PreToolUse hook (Edit|Write): refuses edits to files that tools generate or that
# must not change by hand, and says what to do instead. Windows PowerShell 5.1 compatible.
$ErrorActionPreference = 'Stop'

$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$file = $payload.tool_input.file_path
if (-not $file) { exit 0 }

$parts = $file -split '[\\/]'
$name = $parts[-1]
$reason = $null

if ($parts -contains 'bin' -or $parts -contains 'obj') {
    $reason = 'bin/ and obj/ are build output: change the source or the .csproj and rebuild.'
}
elseif ($name -eq 'packages.lock.json') {
    $reason = 'packages.lock.json is written by NuGet: change the PackageReference, then run dotnet restore --force-evaluate.'
}
elseif ($name -eq 'LICENSE') {
    $reason = 'LICENSE is the AGPL-3.0 text: changing the license is the user''s call, ask them.'
}

if ($reason) {
    @{
        hookSpecificOutput = @{
            hookEventName            = 'PreToolUse'
            permissionDecision       = 'deny'
            permissionDecisionReason = "Blocked by protect-files.ps1: $reason"
        }
    } | ConvertTo-Json -Compress -Depth 3
}
exit 0
