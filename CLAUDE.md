# Elite Dangerous AI (EDAI)

Windows desktop app: tails Elite Dangerous journal files → routes configured events through OpenAI → displays responses + reads them via TTS. Token-conscious — only user-configured events trigger AI calls.

**Artifacts next to executable:** `EDAI.db` (SQLite), `EDAI.log` (flat file), `voice_cache/` (Edge TTS mp3s)

---

## Tech Stack
- C# / .NET 10 · WPF MVVM · CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- MaterialDesignThemes · OpenAI .NET SDK · NAudio
- TTS: `System.Speech.Synthesis` (SAPI, offline) and Edge Neural Voices (WebSocket, no key needed)
- SQLite via EF Core 10 · Microsoft.Extensions.DependencyInjection
- Windows only; `<UseWindowsForms>true</UseWindowsForms>` for FolderBrowserDialog

## Solution Layout
```
EDAI.Core/    — business logic, models, interfaces, journal, pipeline, TTS, OpenAI
EDAI.UI/      — WPF: Views, ViewModels, Controls, Converters, Services, Validators
EDAI.Data/    — EF Core: Entities, Repositories (DpapiHelper lives here), Migrations
```

---

## Critical Rules — Read These First

### Migrations
**Never run `dotnet ef add migration`.** Write manually every time:
1. `YYYYMMDDHHMMSS_Name.cs` — `Up()` / `Down()`
2. `YYYYMMDDHHMMSS_Name.Designer.cs` — full model snapshot at this point
3. Update `EdaiDbContextModelSnapshot.cs`

### Database access
All DB access goes through repositories. **One exception:** `TryApplyEarlyAppearance()` in `App.xaml.cs` uses raw SQL targeting only stable, long-lived columns so the splash theme loads before migrations run.

### Deleting an EventConfiguration
`ResponseLog → EventConfiguration` FK is `DeleteBehavior.Restrict`. Always delete the related `ResponseLog` rows first, then the config — in the same `SaveChangesAsync` call.

### Button styles
Use `EDAI.Button.Primary` (defined in `App.xaml`, `BasedOn MaterialDesignRaisedButton`) — never `MaterialDesignRaisedButton` directly. `EDAI.Brush.ButtonBackground` and `EDAI.Brush.ButtonForeground` must **always** be set in `ApplyAppearance`, never removed — they are `DynamicResource` targets.

### Trigger cooldown
`PipelineOrchestrator` uses `Dictionary<int, DateTime> + lock (_cooldownLock)`, not `ConcurrentDictionary` — a read-check-write sequence isn't atomic without a lock. Both `ProcessAsync` and `ProcessWithConfigAsync` must call `IsCooldownActive`.

### Window shutdown
`ShutdownMode = OnExplicitShutdown`; only `BeginShutdown()` exits. In `OnClosing`: cancel the close, `await SaveWindowStateAsync(...)`, then call `BeginShutdown()` — this re-enters `OnClosing` with `IsShuttingDown = true` which lets it through. **Shift+X always exits** regardless of MinimizeToTray.

---

## Architecture Conventions
- ViewModels have zero WPF type references. Confirmation dialogs: `Func<string, string, bool>? ShowConfirmation` delegate, wired in code-behind to `MessageBox.Show`. Navigation from VMs: `Action? OpenXxxRequested` delegate, same pattern.
- All service contracts in `EDAI.Core/Interfaces/`. All registrations in `App.xaml.cs BuildServiceProvider()`.
- `JournalPathOptions` mutable singleton: set `.Path` after `settingsRepo.GetAsync()` but **before** `watcher.StartAsync()`.
- `ActionQueue` FIFO serializer: all output ops (display, TTS, response log writes) go through it in order.
- API key encrypted with Windows DPAPI (`ProtectedData.CurrentUser`); decrypted in `SettingsRepository.ToModel()`.
- `async/await` throughout — no `.Result` or `.Wait()`. Null safety: `<Nullable>enable</Nullable>` in all projects. One class per file.

---

## Edge TTS — Non-Obvious Auth
The auth tokens must go in **URL query parameters**, not HTTP headers — WinHTTP strips custom headers. Uses `SocketsHttpHandler` via `HttpMessageInvoker` to preserve them.
- `Sec-MS-GEC` = SHA-256(rounded FileTime + TrustedClientToken)
- `ConnectionId` = lowercase no-dash GUID (`Guid.NewGuid().ToString("N")`)
- `Cookie: muid=<uppercase-guid>` header required
- SSML prosody: rate as `+N%` offset from 100%; pitch as `+NHz` offset
- Voice cache key: SHA-256 of `phrase+voiceName+language+rate+pitch`; files sharded into 2-char subdirs under `voice_cache/`

---

## Out of Scope
Course plotting, exobiology tracking, fleet carrier management, trade route optimization, multi-commander, cloud sync, any web/API server.
