# Contributing to Elite Dangerous AI

Thanks for taking the time to contribute. Here is everything you need to get started.

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 10 SDK
- Visual Studio 2022 or Rider (recommended)
- An Elite Dangerous installation (for manual testing)

### Setup
1. Fork the repository and clone your fork
2. Open `EDAI.UI.csproj` or load all three projects in your IDE
3. Copy your OpenAI API key into Settings on first launch
4. The database (`EDAI.db`) and log (`EDAI.log`) are created next to the executable on first run

## How to Contribute

### Reporting Bugs
Open a [GitHub Issue](https://github.com/KarlGrear/EDAI/issues) and include:
- Steps to reproduce
- Expected vs actual behaviour
- Relevant lines from `EDAI.log`
- Your Windows version and .NET runtime version

### Suggesting Features
Open a GitHub Issue with the `enhancement` label. Describe the use case, not just the feature — it helps evaluate fit.

### Submitting a Pull Request
1. Branch off `main` — use a descriptive name e.g. `fix/tts-crash` or `feat/hotkey-support`
2. Keep PRs focused — one bug fix or feature per PR
3. Follow existing patterns:
   - ViewModels use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm
   - No WPF type references in ViewModels
   - All DB access through repositories in `EDAI.Data`
   - New DB columns require a manual migration (see `EDAI.Data/Migrations/`)
4. Update `CHANGELOG.md` — add your change under `## [Unreleased]`
5. Open the PR against `main` and describe what changed and why

## Code Style
- C# conventions — PascalCase for types and members, camelCase for locals
- `<Nullable>enable</Nullable>` is enforced — no nullable warnings
- No comments explaining *what* the code does; only add one if the *why* is non-obvious
- One class per file

## Project Structure
```
EDAI.Core/    — business logic, models, interfaces, pipeline, TTS, OpenAI
EDAI.UI/      — WPF: Views, ViewModels, Controls, Converters, Validators
EDAI.Data/    — EF Core: Entities, Repositories, Migrations
```
