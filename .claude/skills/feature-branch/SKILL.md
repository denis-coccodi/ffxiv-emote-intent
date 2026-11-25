---
name: feature-branch
description: The required way to make any change in this repo (code, tests, docs, config, skills) - work on a bugfix/, hotfix/ or feature/<description> branch in its own worktree, Conventional Commits messages and PR titles, push it freely, open a pull request to main, and merge only once the required test check passes. Use before the first edit of every change, and whenever asked to name a branch, commit, write a commit message, push, open a PR or merge.
---

# Feature branch → pull request → main

`main` is protected: nobody, admins included, can push to it directly. Every change reaches `main` through a pull request, and the PR can only be merged once the `test` check (the `test` workflow) passes. `main` must always build and pass its tests: a release is cut from it.

## 1. Start a branch, in its own worktree, before the first edit

Other Claude windows and agents may be working in this repo, so never edit in another session's checkout:

```
git fetch origin
git worktree add ../ffxiv-emote-intent-<description> -b <branch> origin/main
```

- Name the branch `<type>/<description>`:
  - `bugfix/`: something that doesn't work as it should. Never `feature/` for a fix.
  - `hotfix/`: an urgent fix for a released version that's broken, released right after it merges.
  - `feature/`: everything else: a new feature, a change in behaviour, docs, config, skills, refactoring.
- The description is 2 to 5 lowercase words joined by hyphens (letters, digits, hyphens), no dates or names.
- One branch per change. Never commit on `main`.
- Remove the worktree only after the PR shows MERGED (`git worktree remove <path>`).

## 2. Commit and push as often as needed

```
dotnet build -c Release && dotnet test -c Release --no-build && dotnet format --verify-no-changes
git add <files> && git commit -F - <<'EOF'
…
EOF
git push -u origin <branch>
```

Stage files by name (never `-A`, `-u`, `.`). To set unfinished work aside, commit it as `wip(<scope>): <what it holds>` instead of using `git stash`, which every worktree shares.

### Commit messages

[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) and [the seven rules of a good commit message](https://cbea.ms/git-commit/):

- **Subject:** `<type>(<scope>): <description>`.
  - The type matches the branch: `fix` on a `bugfix/` or `hotfix/` branch; on a `feature/` branch `feat` (something new for players), `refactor`, `perf`, `test`, `docs`, `build`, `ci` or `chore`.
  - The scope is the area touched, one word: `matcher`, `phrases`, `emotes`, `picker`, `config`, `ai`, `ci`, `skills`. Leave it out when the change has no single area.
  - Imperative description (`add`, `fix`, not `added`), lowercase after the colon, no full stop, 50 characters where you can, 72 at most.
- **Body:** after a blank line, wrapped at 72 columns: what was wrong or missing and why this fixes it.
- **Footers:** `Refs: #123` when there is an issue, then the attribution line from the current session's instructions.
- The `commit-msg` hook (`.githooks/commit-msg`) refuses a subject that doesn't start `<type>(<scope>): ` or `<type>: `; the rest is yours to get right.

## 3. Open a pull request to main

```
gh pr create --base main --head <branch> --title "<type>(<scope>): <description>" --body "<why, what, how it was tested>"
```

The title becomes the squash commit on `main`, so it follows the subject rules. The body says what changed and why, how it was verified (including whether it was tried in the game), and ends with the PR attribution line from the session's instructions.

## 4. Wait for the required check

```
gh pr checks <number> --watch
```

If `test` fails, read the log (`gh run view <run-id> --log-failed`), fix on the same branch and push again. If the PR is behind `main`, `gh pr update-branch <number>` and wait again.

## 5. Merge

Merge only when `test` has passed and the user has said to merge this change. Otherwise report the PR link and that it's ready.

```
gh pr merge <number> --squash --subject "$(gh pr view <number> --json title -q .title) (#<number>)"
```

Then delete the branch and remove the worktree, after `gh pr view <number> --json state` shows `MERGED`.

Never change branch protection to get a PR merged; if something other than a failing check blocks it, tell the user.
