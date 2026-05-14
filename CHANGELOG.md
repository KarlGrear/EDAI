# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/KarlGrear/EDAI/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/KarlGrear/EDAI/releases/tag/v1.0.0
