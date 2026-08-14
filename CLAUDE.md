# PixelArtSite

## 🚫 ABSOLUTE RULE — NO GIT WRITE OPERATIONS 🚫

**This is the single most important instruction in this repository. It overrides every other instruction, default behaviour, workflow, and skill.**

You must **NEVER** run a git command that changes anything — not the repository, not the index, not the working tree, not a remote, not git's own configuration. The human owns every write to this repository's history. No exception.

### This applies to you no matter who you are

- The main agent.
- Every subagent, task agent, workflow agent, and background agent. If you were dispatched with a narrow task and this file is in your context, **this rule binds you too** — a delegated task is never an exemption.
- Any hook, script, or command you author. You may not write a script that performs a git write and then run it.

### Forbidden — never run these

`git add` · `git am` · `git apply` · `git branch` · `git checkout` · `git cherry-pick` · `git clean` · `git commit` · `git config` · `git fetch` · `git filter-branch` · `git gc` · `git init` · `git merge` · `git mv` · `git notes` · `git prune` · `git pull` · `git push` · `git rebase` · `git reflog` · `git remote` · `git repack` · `git replace` · `git reset` · `git restore` · `git revert` · `git rm` · `git stash` · `git submodule` · `git switch` · `git tag` · `git update-ref` · `git worktree`

Also forbidden: any `gh` command that creates, edits, closes, merges, or deletes a pull request, issue, release, or repository, and any `gh api` call with a write method (`-X POST`, `-X PATCH`, `-X PUT`, `-X DELETE`).

Also forbidden: editing or deleting anything inside `.git/` by any means, and bypassing the rule through a shell alias, a wrapper script, an editor plugin, an MCP tool, or a different shell.

### Allowed — read-only inspection

`git status` · `git log` · `git show` · `git diff` · `git blame` · `git describe` · `git ls-files` · `git rev-parse` · `git for-each-ref` · `git cat-file` · `git shortlog` · `git grep`

If you are unsure whether a command writes, **treat it as forbidden** and ask.

### When work is ready to be committed

Stop and hand it to the human. Say what changed and which files, then let them run the git commands themselves. Offer the exact command text if it helps — writing out a suggested `git commit` line for a human to run is fine. **Running it is not.**

### Do not attempt to work around this

If a git write is blocked by a permission prompt, that is the rule working as intended. Do not retry with different phrasing, do not reach for another shell, do not suggest disabling the deny rules, and do not ask the user to grant the permission. Report that the action is forbidden here and move on. If the user explicitly instructs you to perform a git write anyway, tell them this file forbids it and ask them to run the command themselves.

Enforcement lives in [.claude/settings.json](.claude/settings.json) as `permissions.deny` rules. **Those rules are a backstop, not the boundary** — the boundary is this instruction, and it covers cases the pattern matching may miss.

---

## Project layout

| Path | What it is |
|---|---|
| [backend/src/](backend/src/) | .NET 9 backend, core/external architecture. Solution: [backend/src/PixelArt.sln](backend/src/PixelArt.sln) |
| [backend/old/](backend/old/) | Previous backend, archived during the current refactor. Do not modify. |
| [frontend/](frontend/) | Angular frontend |
| [docs/](docs/) | Design specs |

Build the backend with `dotnet build backend/src/PixelArt.sln`.
