---
allowed-tools: Bash(git log:*), Bash(git tag:*), Bash(git diff:*), Bash(git show:*), Read, Edit
argument-hint: [version]
description: Update CHANGELOG.md and README.md for a new release based on recent git commits
---

## Task
Update CHANGELOG.md and README.md for a new release version $ARGUMENTS.

## Context
- Recent commits since last tag: !`git log $(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)..HEAD --oneline`
- Last tag: !`git describe --tags --abbrev=0 2>/dev/null || echo "none"`
- Files changed since last tag: !`git diff $(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)..HEAD --stat`
- Existing changelog: @CHANGELOG.md
- Existing readme: @README.md

## Instructions

### Step 0 — Detect duplicate run
Before making any changes, check whether a version entry for $ARGUMENTS already exists in CHANGELOG.md.

- **If $ARGUMENTS is blank** — proceed normally; the target header is `[Unreleased]`, which always exists.
- **If the exact version header `## [$ARGUMENTS]` already appears in CHANGELOG.md** — do not create a duplicate entry. Instead, update the existing entry: add any commits that are not already summarised there, correct anything that has changed, and then continue to Step 2 (README). Make clear in your response that you are updating an existing entry rather than creating a new one.
- **If the version header is absent** — proceed normally with Steps 1 and 2 below.

### Step 1 — Update CHANGELOG.md
1. Analyze the commits above and group them into categories: **Added**, **Changed**, **Fixed**, **Removed**, **Security** (following Keep a Changelog format).
2. Write a new version entry at the top of CHANGELOG.md with today's date and version $ARGUMENTS.
3. Summarize commits in plain English — not raw commit messages. Focus on what changed for the user/developer.
4. Preserve all existing changelog entries below.
5. Update the footer reference links so `[Unreleased]` compares against the new version tag and add a compare link for the new version.
6. If no version argument was given, use "Unreleased" as the version header.

### Step 2 — Update README.md
Cross-reference the new changelog entry against README.md and update every section that is now out of date. Common areas to check:

- **Features list** — add bullet points for new user-facing capabilities; remove or rephrase anything that is no longer accurate.
- **Application Windows** — add entries for any new windows or UI surfaces; update existing window descriptions if their contents changed.
- **Event Configuration Fields** — reflect any new or renamed fields, tabs, or processing modes.
- **Template Engine / Token Prefixes table** — add any new token namespaces (e.g. `session.`) introduced by this release.
- **Condition Evaluator** — update if condition syntax or capabilities changed.
- **Auxiliary File System** — update if new aux files were added.
- **Pipeline Architecture diagram** — update the ASCII diagram if the data flow changed (new processing branches, renamed stages, etc.).
- **Solution Structure** — add new projects or top-level folders that now exist (e.g. `EDAI.Tests/`, `EDAI.Core/Scripting/`).
- **Settings Window** — update the settings table if new settings sections were added.
- **Technology Stack** — add any new dependencies or libraries.

Only edit sections where the content is actually wrong or missing — do not rewrite sections that are still accurate. Preserve the existing style, heading hierarchy, and table formatting.
