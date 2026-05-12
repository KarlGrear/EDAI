# Elite Dangerous AI (EDAI)

## Project Overview
EDAI is a Windows desktop application that monitors Elite Dangerous journal files in real
time, routes configured journal events through OpenAI for AI processing, displays responses
in a text window, and reads them aloud using Text-to-Speech. It is inspired by applications
like EDCopilot but takes a fresh, AI-first, configurable approach. The application is
token-conscious — only user-configured events trigger AI calls.

---

## Application Name
- **Full name:** Elite Dangerous AI
- **Short name:** EDAI
- **Window title:** Elite Dangerous AI
- **Database filename:** EDAI.db (next to the application executable)
- **Log filename:** EDAI.log (next to the application executable)

---

## Tech Stack
- **Language:** C# / .NET 10 (LTS)
- **UI Framework:** WPF with MVVM pattern
- **MVVM Toolkit:** CommunityToolkit.Mvvm (use source generators: [ObservableProperty], [RelayCommand])
- **UI Library:** Material Design in XAML (MaterialDesignThemes NuGet package)
- **AI:** OpenAI .NET SDK (official NuGet package)
- **TTS (SAPI):** System.Speech.Synthesis (Windows built-in, no NuGet required)
- **TTS (Edge):** Microsoft Edge Neural Voices via unofficial WebSocket API (no account/key required)
- **Audio playback:** NAudio (Mp3FileReader + WaveOutEvent) for Edge TTS cached audio
- **Database:** SQLite via EF Core 10 (Microsoft.EntityFrameworkCore.Sqlite)
- **Journal Watching:** System.IO.FileSystemWatcher
- **DI Container:** Microsoft.Extensions.DependencyInjection
- **Platform:** Windows only, .NET 10 Windows target; `<UseWindowsForms>true</UseWindowsForms>` enabled for FolderBrowserDialog

---

## Solution Structure
```
EliteDangerousAI.sln
├── EDAI.Core/               ← Business logic, services, models, interfaces
│   ├── Journal/             ← FileSystemWatcher, journal tail reader, event parser, JournalPathOptions
│   ├── OpenAI/              ← OpenAI client, prompt builder, response parser
│   ├── TTS/                 ← CompositeTtsService, TtsService (SAPI), EdgeTtsService, VoiceCacheService
│   ├── Pipeline/            ← Event pipeline: trigger detection, secondary wait, queue, conditions
│   ├── Models/              ← Shared data models and journal event classes
│   └── Interfaces/          ← All service interfaces (IJournalWatcher, IOpenAIService, etc.)
├── EDAI.UI/                 ← WPF application
│   ├── Views/               ← XAML windows and UserControls
│   ├── ViewModels/          ← MVVM ViewModels
│   ├── Controls/            ← Reusable controls (HsvColorPicker, HelpIcon)
│   ├── Resources/           ← Styles, themes, brushes, icons
│   ├── Converters/          ← XAML value converters
│   ├── Services/            ← UI-layer services (NavigationService, FileDialogService, TrayIconService)
│   └── Validators/          ← Input validation
└── EDAI.Data/               ← EF Core DbContext, entities, repositories
    ├── Entities/            ← EF Core entity classes
    ├── Repositories/        ← Repository pattern implementations (including DpapiHelper)
    └── Migrations/          ← EF Core migrations (manual — do not use dotnet ef add migration)
```

---

## Architecture Pattern
- **MVVM** throughout the UI layer. ViewModels must never reference WPF UI types directly.
- **Repository pattern** in EDAI.Data. Services in EDAI.Core call repositories via interfaces.
- **Dependency Injection** via Microsoft.Extensions.DependencyInjection. All services
  registered at startup in EDAI.UI App.xaml.cs (`BuildServiceProvider`).
- **Async/await** throughout. No blocking calls on the UI thread.
- All service contracts defined as interfaces in EDAI.Core/Interfaces/.
- **Confirmation dialogs in ViewModels:** use a `Func<string, string, bool>? ShowConfirmation`
  delegate property wired by the View's code-behind to `MessageBox.Show` — keeps WPF out of VMs.
- **JournalPathOptions mutable singleton:** injected into `JournalWatcher` and `JournalAuxFileReader`
  via constructor. `App.xaml.cs` sets `Path` after settings load but before `watcher.StartAsync()`.

---

## Database (SQLite)
- **Location:** Same directory as the application executable (EDAI.db)
- **ORM:** EF Core 10 with migrations
- **All application state is persisted to SQLite** — settings, event configurations,
  categories, session history, AI responses, and voice cache
- **Migrations are written manually** — do not use `dotnet ef add migration`. Follow the
  existing pattern: create `YYYYMMDDHHMMSS_Name.cs`, `YYYYMMDDHHMMSS_Name.Designer.cs`,
  and update `EdaiDbContextModelSnapshot.cs`.

### Key Entities

**Settings**
Stores all application settings as a single typed row. Fields:
- `OpenAiApiKeyEncrypted` — Windows DPAPI encrypted; decrypted in `SettingsRepository.ToModel()`
- `OpenAiModel` — selected model (default `gpt-4o`)
- `TtsProvider` — `"SAPI"` or `"EdgeNeural"` (default `"SAPI"`)
- `TtsVoiceName` — SAPI voice name
- `TtsEnabled` — global TTS toggle (default `true`)
- `EdgeTtsLanguage` / `EdgeTtsVoice` / `EdgeTtsRate` / `EdgeTtsPitch` — Edge TTS settings
- `AlwaysOnTop` — window always on top
- `ShowSplashScreen` — show splash on startup (default `true`)
- `TrayNotificationsEnabled` — global tray notification toggle (default `true`)
- `Theme` — `"Light"` or `"Dark"` (default `"Dark"`)
- `PrimaryColor` — Material Design accent hex (default `"#FF6D00"`)
- `CustomBackgroundColor` / `CustomForegroundColor` — optional full-window overrides
- `ToolbarBackground` / `ToolbarForeground` — toolbar color overrides
- `ButtonForeground` — button text color override
- `ControlBackground` / `ControlHoverBackground` / `ControlBorderColor` — form control color overrides
- `FontFamily` / `FontSize` — UI font overrides (default 14pt)
- `WindowWidth` / `WindowHeight` / `WindowLeft` / `WindowTop` / `IsMaximized` — window geometry
- `JournalPath` — path to the Elite Dangerous journal folder (default = current user's standard path)

**Category**
- `Id`, `Name` (unique index)
- User-managed. Used to organize EventConfigurations.

**EventConfiguration**
Core entity. Each record represents one configured AI event pipeline:
- `Id`, `Title` (required), `Description`
- `CategoryId` (FK to Category, nullable)
- `IsEnabled`
- `TriggeringEvents` — JSON array of event type strings (OR logic)
- `TriggerCondition` — optional CEL-style condition expression evaluated against the event JSON
- `TriggerTimeoutMs` — cooldown in milliseconds; if non-zero, suppresses re-trigger within the window
- `SecondaryEvents` — JSON array of event type strings to collect during wait window
- `SecondaryWaitTimeMs` — milliseconds to wait for secondary events (default 1000)
- `SendToAi` — whether to send to OpenAI (when false, only display/TTS output is produced)
- `SendFullTriggerEvent` — include full raw trigger event JSON in the prompt
- `ModelOverride` — per-config OpenAI model (overrides global setting when set)
- `Prompt` — user-defined prompt template
- `ExpectedResultsSchema` — JSON schema template for AI response format
- `DisplayTitle` / `AnnounceTitle` — whether to show/speak the config title in output
- `DisplayFields` — JSON array of field keys to show in UI
- `DisplayKeys` — show "Key: Value" vs "Value" only
- `DisplayCondition` — optional condition expression controlling whether display fires
- `AnnounceFields` — JSON array of field keys to speak via TTS
- `AnnounceKeys` — announce "Key: Value" vs "Value" only
- `AnnounceCondition` — optional condition expression controlling whether TTS fires
- `ShowTrayNotification` — per-config tray balloon toggle
- `CreatedAt`, `UpdatedAt`

**SessionHistory**
One record per play session (new journal file = new session):
- `Id`, `CommanderName`, `SessionStart`, `SessionEnd`, `JournalFileName`

**ResponseLog**
One record per AI response fired:
- `Id`, `SessionHistoryId` (FK, nullable), `EventConfigurationId` (FK)
- `Timestamp`
- `TriggeringEventJson` — raw journal JSON that triggered
- `SecondaryEventsJson` — collected secondary events JSON array
- `PromptSent` — full prompt sent to OpenAI
- `RawAiResponse` — full JSON string returned by OpenAI
- `DisplayedOutput` — formatted string shown in UI
- `AnnouncedOutput` — formatted string sent to TTS

**VoiceCache**
One record per unique cached Edge TTS audio file:
- `Hash` (PK) — SHA-256 of `phrase + voiceName + language + rate + pitch`
- `Phrase`, `VoiceName`, `Language`, `Rate`, `Pitch`
- `FilePath` — path to the `.mp3` file on disk (2-char shard directory under `voice_cache/`)
- `CreatedAt`, `LastUsed`, `UseCount`

---

## Elite Dangerous Journal Files
- **Default location:** `%USERPROFILE%\Saved Games\Frontier Developments\Elite Dangerous\`
- **Configurable:** user can change the folder in Application Settings; stored in `Settings.JournalPath`
- **Format:** One JSON object per line, appended in real time while the game runs
- **File naming:** `Journal.YYYY-MM-DDTHHMMSS.NN.log`
- **Active file:** Always the most recently modified `.log` file in the directory
- Use `FileSystemWatcher` to detect file changes, then tail-read new lines only
- Each line is a complete JSON object with a mandatory `event` field identifying the type
- Auxiliary JSON files (Market.json, Status.json, etc.) read by `JournalAuxFileReader`

---

## Event Processing Pipeline

### Overview
EDAI reads all journal events but only acts on events that match a configured
EventConfiguration. The pipeline is token-conscious — unmatched events are silently
discarded. All processing is async and non-blocking.

### Pipeline Steps
1. **JournalWatcher** tails the active journal file; emits raw JSON lines via event
2. **JournalParser** deserializes each line, extracts the `event` type field
3. **TriggerMatcher** checks if the event type matches any enabled EventConfiguration's
   `TriggeringEvents` list (OR logic), then evaluates `TriggerCondition` if set
4. **TriggerTimeout check** — if `TriggerTimeoutMs > 0` and the config was triggered within
   the cooldown window, the trigger is suppressed and logged with time-remaining message
5. **SecondaryEventCollector** waits `SecondaryWaitTimeMs` ms, collecting any events whose
   types appear in `SecondaryEvents`
6. **PromptBuilder** assembles the final prompt: system persona + user prompt template
   + triggering event JSON (with optional `SendFullTriggerEvent`) + secondary events JSON
   + `ExpectedResultsSchema` instruction
7. **OpenAIService** sends the prompt; uses `ModelOverride` if set, else global `OpenAiModel`
8. **ResponseParser** extracts `DisplayFields` and `AnnounceFields` from the JSON response
9. **OutputDispatcher** sends output to:
   - UI response window (if `DisplayFields` configured and `DisplayCondition` passes)
   - TTS service (if `AnnounceFields` configured, TTS enabled, and `AnnounceCondition` passes)
   - Windows tray balloon (if app is minimized AND `ShowTrayNotification` AND global toggle on)
10. **ResponseLog** record written to SQLite

### AI Persona / System Prompt
The AI acts as a ship's onboard computer AI. The system prompt establishes this persona and
instructs the model to respond only with the requested JSON object. The user-defined `Prompt`
field per EventConfiguration is appended after the persona.

### Expected Results Schema
A JSON template the user defines, e.g. `{"system_name": "", "threat_level": "", "recommendation": ""}`.
Embedded in the prompt to instruct the AI on exact response format.

---

## OpenAI Integration
- Official OpenAI .NET SDK
- Model user-selectable globally in Settings; overridable per EventConfiguration (`ModelOverride`)
- API key encrypted with Windows DPAPI (`ProtectedData.DataProtectionScope.CurrentUser`)
- Each prompt is a fresh stateless call — no conversation history
- JSON response format requested where supported

---

## Text-to-Speech

EDAI supports two TTS engines, switchable in Settings:

### Windows Speech (SAPI)
- `System.Speech.Synthesis` — offline, no latency, uses installed Windows voices
- Voice selectable from installed SAPI voices; "Test Voice" button auditions before saving

### Edge Neural Voices
- Microsoft's cloud neural engine via the same WebSocket backend as Edge Read Aloud
- No account or API key required; requires internet connection
- `EdgeTtsService` connects to `wss://speech.platform.bing.com/...` using `TrustedClientToken`
- Authentication: `Sec-MS-GEC` token (SHA-256 of rounded FileTime + TrustedClientToken) and
  `Sec-MS-GEC-Version` passed as **URL query parameters** (not HTTP headers)
- `ConnectionId` is a lowercase no-dash GUID (`Guid.NewGuid().ToString("N")`)
- `Cookie: muid=<uppercase-guid>` header required
- SSML prosody: rate as `+N%` offset from 100%; pitch as `+NHz` offset
- Uses `SocketsHttpHandler` via `HttpMessageInvoker` to bypass WinHTTP stripping custom headers

### Voice Cache (Edge TTS only)
- `VoiceCacheService` caches synthesized audio to disk as `.mp3` files
- Cache directory: `voice_cache/` next to the executable; files stored in 2-char shard subdirectories
- Cache key: SHA-256 of `phrase + voiceName + language + rate + pitch`
- Playback via NAudio (`Mp3FileReader + WaveOutEvent`)
- Log messages indicate cache hit vs. API call
- "Clear Voice Cache" button in Settings deletes all `.mp3` files AND all `VoiceCache` DB rows
  independently (each step succeeds/fails on its own)

### Common TTS behaviour
- `CompositeTtsService` routes calls to the active engine based on `ActiveProvider`
- Global enabled/disabled toggle persisted to SQLite
- TTS runs on a background thread; announcements are queued if TTS is busy

---

## UI Themes and Appearance
- Material Design in XAML (MaterialDesignThemes) for all controls
- Light / Dark base theme, persisted to SQLite
- Primary accent color configurable (default Elite Dangerous orange `#FF6D00`)
- **Theme Customization window** (`ThemeWindow`) provides HSV color pickers for:
  - Toolbar background / foreground
  - Button foreground
  - Custom window background / foreground
  - Control background / hover background / border color
- All color overrides persisted to SQLite and applied immediately via `App.ApplyAppearance()`
- Font family and font size configurable in Application Settings

---

## Window Behavior
- **Splash screen** shown on startup during DB init and service startup (5-second minimum)
- **Minimize to system tray** — closing the window minimizes to tray, does not exit;
  right-click tray icon: Restore, Exit
- **Always on Top** — toggleable from toolbar and Settings, persisted to SQLite
- **Window size and position** — saved on close/minimize, restored on launch
- **Position clamping** — saved position is validated against `SystemParameters.VirtualScreenLeft/Top/Width/Height`
  before applying; falls back to `CenterOnPrimaryMonitor()` if off-screen (guards against
  positions from monitors that are no longer connected)
- **Background processing** — journal watching and AI pipeline continue when minimized to tray

---

## Tray Notifications
- Balloon shown when app is minimized to tray AND `ShowTrayNotification` is true for the
  config AND global `TrayNotificationsEnabled` is on
- Notification title = config `Title`; body = truncated `DisplayedOutput` or `AnnouncedOutput`

---

## Error Handling
- **All errors written to EDAI.log** (next to executable) — always, regardless of severity
- **Minor errors** (TTS failure, single journal parse error): shown in main window status bar
- **Critical errors** (DB connection failure, OpenAI auth failure, journal directory not found):
  pop-up dialog AND status bar update AND log
- All exceptions caught at service boundaries and routed through `IErrorService`
- Log format: `[TIMESTAMP] [LEVEL] [SOURCE] Message`

---

## UI Screens

### Main Window
- **Left panel — TabControl with two tabs:**
  - **AI Responses tab** — scrolling list of `ResponseDisplayItem` cards; each card shows
    optional config title, formatted response text, and timestamp
  - **History tab** — last 100 `ResponseLog` entries loaded from DB on startup;
    each item is an `Expander` showing timestamp, config title, and truncated summary in
    the header; expanded view shows full `PromptSent` and `RawAiResponse` in read-only
    Consolas text boxes
- **Right panel** — Event Log: scrollable list of journal events that fired triggers
  (event type + timestamp)
- **Toolbar buttons:** Settings, Event Configurations, Test, Always On Top toggle,
  TTS On/Off toggle, Theme Customization (palette icon), Clear Responses, About
- **Status bar:** status message (left), error message (right)

### Event Configuration Selection Screen
- Filterable list of all EventConfigurations
- Filter by: Category (dropdown + "Manage Categories" button)
- Search by: Primary Trigger, Secondary Trigger, Title, or All
- Each row: Enabled checkbox (with inline save/cancel confirm), Title, Category, Triggers summary
- New Config button; double-click or Edit button opens Edit screen

### Event Configuration Edit Screen
Tabbed form (Trigger / Output / Advanced tabs):

**Trigger tab:**
- Title, Description, Category (+ Manage Categories button), IsEnabled
- Trigger Cooldown: numeric value + unit dropdown (Milliseconds / Seconds / Minutes / Hours)
- Triggering Events (multi-value chip input — OR logic)
- Trigger Condition (optional expression)
- Secondary Events (multi-value chip input)
- Secondary Wait Time Ms

**Output tab:**
- Send to AI checkbox; Send Full Trigger Event checkbox
- Model Override (optional, falls back to global)
- Prompt (multi-line text area)
- Expected Results Schema (multi-line text area)
- Title Display Mode (Display checkbox + Announce checkbox)
- Display Fields / Display Keys / Display Condition
- Announce Fields / Announce Keys / Announce Condition
- Show Tray Notification

**Save / Cancel / Delete** (Delete requires confirmation)

### Category Management Screen
- List of categories with Add, Rename, Delete
- Delete warns if category is in use

### Application Settings Screen
- **OpenAI:** API Key (masked), Model (editable dropdown)
- **Text-to-Speech:**
  - TTS Enabled toggle
  - Voice Engine dropdown (Windows Speech / Edge Neural Voices)
  - *SAPI panel:* Voice dropdown + Test Voice button
  - *Edge Neural panel:* Language dropdown, Rate slider, Pitch slider, Voice dropdown + Test Voice button, Clear Voice Cache button
- **Notifications:** Tray Notifications Enabled toggle
- **Interface:** Always on Top, Show Splash Screen on Launch, Font Family, Font Size slider
- **Journal:** Journal Folder path text box + Browse… button (FolderBrowserDialog)
- Save / Cancel buttons

### Theme Customization Screen (`ThemeWindow`)
- Base theme toggle (Light / Dark)
- Primary accent color HSV picker
- Custom background / foreground color pickers
- Toolbar background / foreground color pickers
- Button foreground color picker
- Control background / hover / border color pickers
- Apply and Cancel buttons; changes take effect immediately via `App.ApplyAppearance()`

### Test Screen
- Multi-line text area to paste raw journal JSON lines
- Run Test button — processes pasted input through the full pipeline (secondary wait, OpenAI, display, TTS)
- Output area mirrors the main window AI Responses tab
- Useful for building and validating new EventConfiguration entries

### About Screen
- Application name, version, and general information

---

## Key Service Patterns

### JournalPathOptions
Mutable singleton in `EDAI.Core.Journal`. Carries the journal directory path from settings
into `JournalWatcher` and `JournalAuxFileReader`. Set in `App.xaml.cs` after
`settingsRepo.GetAsync()` but before `watcher.StartAsync()`.

### IFileDialogService / FileDialogService
Abstraction layer over Win32 file/folder dialogs (`Microsoft.Win32.OpenFileDialog`,
`SaveFileDialog`, `System.Windows.Forms.FolderBrowserDialog`). Keeps ViewModels free
of WPF/WinForms types.

### ActionQueue
FIFO serialiser for display, TTS, and response-log writes. Ensures all output operations
happen in order without blocking the pipeline.

### ConfigExportService
Exports/imports EventConfiguration records to/from JSON files using `IFileDialogService`.

---

## Coding Conventions
- Use `async/await` throughout — no `.Result` or `.Wait()` calls
- Use CommunityToolkit.Mvvm source generators:
  `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]`
- ViewModels must have zero references to WPF UI types (no Window, UserControl, MessageBox, etc.)
- Use `Func<string, string, bool>? ShowConfirmation` delegate pattern for confirmation dialogs
  in ViewModels — wired by the View's code-behind to `MessageBox.Show`
- All service calls go through interfaces defined in EDAI.Core/Interfaces/
- Register all services in the DI container in App.xaml.cs `BuildServiceProvider()`
- Use EF Core repository pattern — no raw SQL, no DbContext calls outside repositories
  (exception: `TryApplyEarlyAppearance()` in App.xaml.cs uses raw SQL targeting only stable columns)
- Use `ILogger<T>` for all logging, routed to the flat file logger (EDAI.log)
- Null safety: `<Nullable>enable</Nullable>` in all csproj files
- One class per file. File name matches class name.
- Migrations are written manually — do not run `dotnet ef add migration`

---

## Out of Scope (Do Not Implement)
- Course plotting or navigation assistance
- Exobiology tracking
- Fleet carrier management (future phase)
- Trade route optimization (future phase)
- Multi-commander configurations
- Cloud sync or remote access
- Any web or API server component
