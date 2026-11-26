---
name: release
description: How a version of Emote Intent reaches players - bump Version in a pull request, tag the merged commit of main vX.Y.Z.W, and the release workflow publishes latest.zip and the repo.json feed players add in Dalamud. Use when asked to release, publish, ship to players, cut a version, or when a release run fails or the feed looks wrong.
---

# Releasing a version

Players install Emote Intent from our custom plugin repository: they add this feed once in `/xlsettings` → Experimental → Custom Plugin Repositories, and Dalamud offers each new release as an update.

```
https://github.com/denis-coccodi/ffxiv-emote-intent/releases/latest/download/repo.json
```

The feed is an asset of the latest GitHub release, so it moves with each release and nothing is committed to `main` to publish one.

**Release only when the user says so**: a release reaches every player who installed the plugin.

## 1. Bump the version in a pull request

`Version` in `EmoteIntent/EmoteIntent.csproj` (four parts, `major.minor.patch.revision`): a new feature bumps minor, a fix patch. Usually the PR that makes the change bumps it already; otherwise a `chore(release): 0.18.0.0` PR (`feature-branch` skill). The README and the plugin's `Description` in the csproj must say what this version does.

## 2. Tag the merged commit of main

```
git fetch origin
git tag v<Version> origin/main
git push origin v<Version>
```

The tag is `v` + the exact `Version` (`v0.17.0.1`). Tag `origin/main` after the bump has merged, never a branch.

## 3. The release workflow publishes it

`.github/workflows/release.yaml` runs on the tag, on Windows like `test`:

1. Refuses a tag that isn't on `main`.
2. Builds, tests and checks formatting.
3. Refuses a tag that isn't the plugin's version (`EmoteIntent.json`'s `AssemblyVersion`).
4. Creates the GitHub release `Emote Intent <version>` with `latest.zip` (DalamudPackager's zip) and notes generated from the PRs merged since the last release.
5. Writes `repo.json` with `tools/New-RepoJson.ps1` (the manifest, the zip's download link, the time and the notes as the changelog) and attaches it.

Watch it with `gh run watch` (`gh run list --workflow release`). In the game, `/xlplugins` → All Plugins → Emote Intent should then offer the new version (Dalamud checks the feed when the installer opens; "Update plugins" to force it).

## When something goes wrong

- **The run failed before "Publish the release"**: nothing was published. Delete the tag (`git push origin :refs/tags/v<Version>` and `git tag -d v<Version>`), fix through a PR, tag again.
- **The release exists but has no repo.json**: rerun the failed job (`gh run rerun <id> --failed`) after deleting the release (`gh release delete v<Version> --yes`), or run `tools/New-RepoJson.ps1` by hand and `gh release upload v<Version> repo.json`.
- **A released version is broken**: a `hotfix/` branch, bump the patch, merge, tag. Never move or reuse a tag that players may have downloaded.
- `InternalName` / `AssemblyName` `EmoteIntent` never changes: Dalamud would see a different plugin.
