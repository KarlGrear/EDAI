# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-05-18

### Added
- Scripting engine — write C# scripts (via Roslyn) as condition expressions (trigger, display, announce) or as AI-replacement processors; scripts receive the journal event, collected secondary events, game state files, and a shared session store as globals
- Script Designer window — AvalonEdit editor with syntax highlighting, background compilation, error feedback, and a globals reference panel
- Session service — persistent key-value store (`session.json`) readable and writable from scripts and accessible in prompt templates via `|session.key|` tokens
- Security sandbox — syntax walker blocks unsafe APIs; opt-in permission gates for file system, network, process execution, and reflection access configurable in Settings
- `EDAI.Tests` project — unit test coverage for journal parsing, trigger matching, condition evaluation, prompt building, template engine, OpenAI response parsing, and scripting service validation and execution
- GitHub community files: structured issue templates, Dependabot configuration, `CONTRIBUTING.md`, `LICENSE`, and `SECURITY.md`
- Built-in preset: NextStarNotScoopable event configuration example

### Changed
- Event processing mode is now a `ProcessingType` enum (`None` / `AI` / `Script`) replacing the boolean `SendToAi` flag; existing configurations load unchanged (absent field treated as `AI` when `sendToAi` was true)
- Syntax highlighting colours for the script editor are configurable via the theme window
- GitHub Actions workflows opt into Node.js 24 to resolve Node.js 20 deprecation warnings

### Fixed
- Pipeline test screen updated to reflect pipeline processing changes introduced with the scripting engine

## [1.0.0] - 2026-05-13

### Added
- Initial release of Elite Dangerous AI
- Journal file watcher — tails Elite Dangerous journal files in real time
- AI pipeline — routes configured journal events through OpenAI and displays responses
- Text-to-speech output via SAPI (offline) and Edge Neural Voices
- Event configuration — per-event AI prompts, triggers, cooldowns, and TTS settings
- Category management for grouping event configurations
- Theme engine — customisable primary, background, toolbar, button, and text colours
- Settings — journal path, API key (DPAPI-encrypted), voice selection, minimise-to-tray
- Pipeline test window — paste raw journal JSON and preview AI responses
- Template tester — evaluate prompt templates and conditions against sample data
- Import / export of event configurations
- Window state persistence — size, position, and maximised state saved across sessions
- Minimise to tray with system tray icon; Shift+close always exits

[Unreleased]: https://github.com/KarlGrear/EDAI/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/KarlGrear/EDAI/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/KarlGrear/EDAI/releases/tag/v1.0.0
