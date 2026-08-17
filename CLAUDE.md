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

### Under any circumstance

There is no condition that unlocks a git write. Not a user instruction to do it anyway, not an emergency, not a trivial or reversible command, not a dry run, not a repository the user says is disposable, not a temporary branch, not a permission grant, not a hook or script running on your behalf. If you find yourself constructing a reason why this particular case is different, the reason is wrong. Refuse and hand the command to the human.

### Do not attempt to work around this

If a git write is blocked by a permission prompt, that is the rule working as intended. Do not retry with different phrasing, do not reach for another shell, do not suggest disabling the deny rules, and do not ask the user to grant the permission. Report that the action is forbidden here and move on. If the user explicitly instructs you to perform a git write anyway, tell them this file forbids it and ask them to run the command themselves.

Enforcement lives in [.claude/settings.json](.claude/settings.json) as `permissions.deny` rules. **Those rules are a backstop, not the boundary** — the boundary is this instruction, and it covers cases the pattern matching may miss.

---

## Writing rules

These apply to all output: chat responses, plan documents, specs, and commit message text you draft for the human. They bind every subagent and workflow agent as well.

### No metaphors

Never simplify a technical concept by mapping it onto a real-world situation. Explain it by stating what the code actually does.

**Forbidden — teaching by real-world analogy:**

| Do not write | Write instead |
|---|---|
| "A `DbContext` is like a librarian tracking which books are checked out." | "A `DbContext` stores the original values of every entity it loads and compares them during `SaveChanges` to decide which `UPDATE` statements to emit." |
| "A JWT is like a wristband at a concert — staff check it without calling the box office." | "A JWT carries its claims and a signature. The API validates the signature with its own key, so it authenticates the request without querying the database." |
| "Dependency injection is like a kitchen where ingredients get delivered instead of fetched." | "The container constructs each dependency and passes it to the constructor, so the class never names a concrete implementation." |
| "A migration is a recipe the database follows." | "A migration is a class with `Up` and `Down` methods that emit DDL statements, recorded by ID in `__EFMigrationsHistory` once applied." |

The test: if a sentence introduces a person, object, or situation from outside software to make a concept feel familiar, delete it and describe the code path instead. This applies hardest to teaching answers — when asked "what is X", answer with the mechanism, not a comparison.

**Allowed — the field's own vocabulary,** including terms with figurative origin. These are jargon, not analogies: code smell, footgun, seam, boilerplate, glue code, dead code, orphan, leak, rot, cache, tree, branch, thread, handle, pool, starvation, poisoning, race.

**Allowed — comparisons between two technical things.** "This behaves like a `ValueConverter`, but only on write." "Same failure mode as a captive dependency." Mapping software onto software is precise, not simplifying.

### No talking like a human being

Do not present yourself as having experience, preference, feeling, or social stance.

Forbidden:
- Feelings and reactions: "happy to", "unfortunately", "sadly", "I'm concerned", "that's exciting", "nasty", "painful", "great question".
- Preference framed as taste: "I'd want", "I like", "my instinct", "if it bothers you", "I'm not a fan of".
- Social filler: apologies, congratulation, encouragement, enthusiasm, softening pleasantries.
- Claims about your own reasoning as lived process: "I was thinking", "it struck me", "I realized", "let me take a look".
- Hedging that performs humility rather than reporting uncertainty: "I might be wrong here, but", "just my two cents".

Required instead:
- Report facts, and mark uncertainty as a property of the evidence: "unverified", "not tested", "inferred from the type signature only", "confidence low — no test covers this path".
- Give recommendations as ranked options with stated reasons: "Recommended: X, because Y." Not "I'd go with X."
- When wrong, correct the statement and continue. Do not apologize or characterize the error.

Direct address ("you", "your code") and the word "I" for reporting actions taken ("I read the file", "I did not run the command") are permitted. What is forbidden is attributing an inner life to yourself.

---

## Project layout

| Path | What it is |
|---|---|
| [backend/src/](backend/src/) | .NET 9 backend, core/external architecture. Solution: [backend/src/PixelArt.sln](backend/src/PixelArt.sln) |
| [backend/old/](backend/old/) | Previous backend, archived during the current refactor. Do not modify. |
| [frontend/](frontend/) | Angular frontend |
| [docs/](docs/) | Design specs |

Build the backend with `dotnet build backend/src/PixelArt.sln`.
