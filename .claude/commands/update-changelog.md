---
allowed-tools: Bash(git log:*), Bash(git tag:*), Bash(git diff:*), Read, Edit
argument-hint: [version]
description: Update CHANGELOG.md for a new release based on recent git commits
---

## Task
Update CHANGELOG.md for a new release version $ARGUMENTS.

## Context
- Recent commits since last tag: !`git log $(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)..HEAD --oneline`
- Last tag: !`git describe --tags --abbrev=0 2>/dev/null || echo "none"`
- Current diff summary: !`git diff HEAD --stat`
- Existing changelog: @CHANGELOG.md

## Instructions
1. Analyze the commits above and group them into categories: **Added**, **Changed**, **Fixed**, **Removed**, **Security** (following Keep a Changelog format).
2. Write a new version entry at the top of CHANGELOG.md with today's date and version $ARGUMENTS.
3. Summarize commits in plain English — not raw commit messages. Focus on what changed for the user/developer.
4. Preserve all existing changelog entries below.
5. If no version argument was given, use "Unreleased" as the version header.
