# Elite Dangerous AI (EDAI)

## Project Overview
EDAI is a Windows desktop application that monitors Elite Dangerous journal files in real
time, routes configured journal events through OpenAI for AI processing, displays responses
in a text window, and reads them aloud using Windows Text-to-Speech. It is inspired by
applications like EDCopilot but takes a fresh, AI-first, configurable approach. The
application is token-conscious — only user-configured events trigger AI calls.

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
- **TTS:** System.Speech.Synthesis (Windows built-in, no NuGet required)
- **Database:** SQLite via EF Core 10 (Microsoft.EntityFrameworkCore.Sqlite)
- **Journal Watching:** System.IO.FileSystemWatcher
- **DI Container:** Microsoft.Extensions.DependencyInjection
- **Platform:** Windows only, .NET 10 Windows target

---

## Solution Structure
```
EliteDangerousAI.sln
├── EDJP.Core/               ← Business logic, services, models, interfaces
│   ├── Journal/             ← FileSystemWatcher, journal tail reader, event parser
│   ├── OpenAI/              ← OpenAI client, prompt builder, response parser
│   ├── TTS/                 ← TTS service wrapper (System.Speech.Synthesis)
│   ├── Pipeline/            ← Event pipeline: trigger detection, secondary wait, queue
│   ├── Models/              ← Shared data models and journal event classes
│   └── Interfaces/          ← All service interfaces (IJournalWatcher, IOpenAIService, etc.)
├── EDJP.UI/                 ← WPF application
│   ├── Views/               ← XAML windows and UserControls
│   ├── ViewModels/          ← MVVM ViewModels
│   ├── Resources/           ← Styles, themes, brushes, icons
│   └── Converters/          ← XAML value converters
└── EDJP.Data/               ← EF Core DbContext, entities, repositories
    ├── Entities/            ← EF Core entity classes
    ├── Repositories/        ← Repository pattern implementations
    └── Migrations/          ← EF Core migrations
```

---

## Architecture Pattern
- **MVVM** throughout the UI layer. ViewModels must never reference UI controls directly.
- **Repository pattern** in EDJP.Data. Services in EDJP.Core call repositories via interfaces.
- **Dependency Injection** via Microsoft.Extensions.DependencyInjection. All services
  registered at startup in EDJP.UI App.xaml.cs.
- **Async/await** throughout. No blocking calls on the UI thread.
- All service contracts defined as interfaces in EDJP.Core/Interfaces/.

---

## Database (SQLite)
- **Location:** Same directory as the application executable (EDAI.db)
- **ORM:** EF Core 10 with migrations
- **All application state is persisted to SQLite** — settings, event configurations,
  categories, session history, and AI responses

### Key Entities

**Settings**
Stores all application settings as key/value pairs or as a typed entity. Includes:
- OpenAI API key (encrypted via Windows DPAPI before storage)
- Selected OpenAI model
- Selected TTS voice name
- TTS enabled/disabled global flag
- Always On Top (persistent)
- Light/Dark theme preference
- Window size and position (last known)
- Tray notification global override (enabled/disabled)

**Category**
- Id, Name
- User-managed. Used to organize event configurations.
- Managed via a dedicated Category Management UI.

**EventConfiguration**
Core entity. Each record represents one configured AI event pipeline:
- Id
- Title (required)
- Description (optional)
- CategoryId (FK to Category)
- IsEnabled (bool, toggled on selection screen or edit screen)
- TriggeringEvents (stored as JSON array of event type strings — OR logic)
- SecondaryEvents (stored as JSON array of event type strings)
- SecondaryWaitTimeMs (int, milliseconds to wait for secondary events)
- Prompt (string, the OpenAI system/user prompt template)
- ExpectedResultsSchema (string, JSON template defining expected AI response shape)
- TitleDisplayMode (enum: None, Display, Announce, Both)
- DisplayFields (stored as JSON array of field key strings)
- DisplayKeys (bool — if true show "Key: Value", if false show "Value" only)
- AnnounceFields (stored as JSON array of field key strings)
- AnnounceKeys (bool — if true announce "Key: Value", if false announce value only)
- ShowTrayNotification (bool, per-config tray notification setting)
- CreatedAt, UpdatedAt

**SessionHistory**
One record per play session (detected by new journal file):
- Id, CommanderName, SessionStart, SessionEnd, JournalFileName

**ResponseLog**
One record per AI response fired:
- Id, SessionHistoryId (FK), EventConfigurationId (FK), Timestamp
- TriggeringEventJson (raw journal JSON that triggered)
- SecondaryEventsJson (raw journal JSON array of secondary events collected)
- PromptSent (full prompt string sent to OpenAI)
- RawAIResponse (full JSON string returned by OpenAI)
- DisplayedOutput (formatted string shown in UI)
- AnnouncedOutput (formatted string sent to TTS)

---

## Elite Dangerous Journal Files
- **Location:** `%USERPROFILE%\Saved Games\Frontier Developments\Elite Dangerous\`
- **Format:** One JSON object per line, appended in real time while the game runs
- **File naming:** `Journal.YYYY-MM-DDTHHMMSS.NN.log`
- **Active file:** Always the most recently modified `.log` file in the directory
- Use `FileSystemWatcher` to detect file changes, then tail-read new lines only
- Each line is a complete JSON object with a mandatory `event` field identifying the type

---

## Event Processing Pipeline

### Overview
EDAI reads all journal events but only acts on events that match a configured
EventConfiguration. The pipeline is token-conscious — unmatched events are silently
discarded. All processing is async and non-blocking.

### Pipeline Steps
1. **Journal Watcher** tails the active journal file and emits raw JSON lines
2. **Event Parser** deserializes each line, extracts the `event` type field
3. **Trigger Matcher** checks if the event type matches any enabled
   EventConfiguration's TriggeringEvents list (OR logic across multiple triggers)
4. **Secondary Event Collector** starts a timer (SecondaryWaitTimeMs). During the
   wait window, any journal events matching the config's SecondaryEvents list are
   collected and held
5. **Queue** — if the same trigger fires again while a pipeline instance is in
   progress for the same config, the new trigger is queued and processed in order
   after the current one completes
6. **Prompt Builder** assembles the final prompt: system persona + user prompt
   template + triggering event JSON + collected secondary events JSON +
   ExpectedResultsSchema instruction
7. **OpenAI Client** sends the prompt and receives the JSON response
8. **Response Parser** extracts DisplayFields and AnnounceFields from the JSON
   response per the config rules
9. **Output Dispatcher** sends formatted output to:
   - UI response window (if display fields configured)
   - TTS service (if announce fields configured and TTS enabled)
   - Windows tray notification (if app is minimized AND per-config AND global
     override not disabled)
10. **ResponseLog** record written to SQLite

### AI Persona / System Prompt
The AI acts as a ship's onboard computer AI. The system prompt establishes this
persona and instructs the model to respond only with the requested JSON object and
nothing else. Example system prompt prefix:
> "You are EDAI, the onboard computer AI of an Elite Dangerous commander. You have
> access to ship telemetry, navigation data, and galactic knowledge. Respond only
> with a valid JSON object matching the schema provided. Do not include explanation,
> markdown, or any text outside the JSON object."

The user-defined Prompt field per EventConfiguration is appended after the persona
as the specific instruction for that event type.

### Expected Results Schema
The ExpectedResultsSchema field is a JSON template the user defines in the config UI,
for example:
```json
{"system_name": "", "threat_level": "", "recommendation": ""}
```
This schema is embedded in the prompt to instruct the AI on the exact response format.
EDAI then parses the AI's JSON response against this schema to extract fields.

---

## OpenAI Integration
- Use the official OpenAI .NET SDK
- Model is user-selectable in Settings (e.g. gpt-4o, gpt-4o-mini, etc.)
- API key stored in SQLite, encrypted with Windows DPAPI
  (use `System.Security.Cryptography.ProtectedData` with `DataProtectionScope.CurrentUser`)
- Each prompt is sent as a fresh stateless call — no conversation history sent to API
- The prompt itself provides all necessary context (persona + event data + schema)
- Always request JSON response format where supported by the selected model

---

## Text-to-Speech
- Use `System.Speech.Synthesis` (Windows built-in, no external dependency)
- User selects from installed Windows SAPI voices in Settings
- TTS has a global enabled/disabled toggle (persisted to SQLite)
- Per-config AnnounceFields and AnnounceKeys control what is spoken
- TTS runs on a background thread — never block UI thread
- If TTS is busy when a new announcement arrives, queue it

---

## UI Themes
- Material Design in XAML (MaterialDesignThemes) for all controls
- User-switchable Light / Dark theme, persisted to SQLite
- Theme toggle accessible from main window (button or menu item)
- Use Material Design color palette — no custom color hardcoding except
  Elite Dangerous accent color applied as the primary palette hue

---

## Window Behavior
- **Splash screen** shown on application startup during DB init and service startup
- **Minimize to system tray** — closing the window minimizes to tray, does not exit
  Right-click tray icon menu: Restore, Exit
- **Always on Top** — toggleable, persisted to SQLite
- **Window size and position** — saved to SQLite on close/minimize, restored on launch
- **Background processing** — journal watching and AI pipeline continue running
  when minimized to tray

---

## Tray Notifications
- When app is minimized to tray and an AI response fires:
  - Show Windows balloon/toast notification IF per-config ShowTrayNotification is true
    AND global tray notification override is not disabled
- Global override in Settings can disable all tray notifications regardless of per-config setting
- Notification shows the config Title and a brief summary of the response

---

## Error Handling
- **All errors written to EDAI.log** (next to executable) — always, regardless of severity
- **Minor errors** (TTS failure, single journal parse error, non-critical): shown in
  main window status bar only
- **Critical errors** (DB connection failure, OpenAI auth failure, journal directory
  not found): pop-up dialog shown to user AND status bar updated AND written to log
- Never crash silently. All exceptions caught at service boundaries and routed through
  a central IErrorService
- Log format: `[TIMESTAMP] [LEVEL] [SOURCE] Message`

---

## UI Screens

### Main Window
- AI response display area (scrolling, shows Title if TitleDisplayMode includes Display,
  then formatted response fields)
- Triggering event log (shows event type and timestamp when a trigger fires)
- Status bar: connection status, last event received, error indicator
- Theme toggle (Light/Dark)
- Always On Top toggle
- TTS mute/unmute button
- Navigation to Settings, Event Configuration, and Test screens

### Event Configuration Selection Screen
- Filterable list of all EventConfigurations
- Filter by: Category (dropdown with "Manage Categories" button beside it)
- Search by: Primary Trigger, Secondary Trigger, Title, or All
- Each row shows: Enabled checkbox, Title, Category, Triggering Events summary
- Enabled checkbox on this screen: toggling it reveals a green checkmark (save) and
  red X (cancel) button inline on that row — does not auto-save
- Button to open new EventConfiguration edit screen
- Double-click or Edit button opens existing config in edit screen

### Event Configuration Edit Screen
- Full form for all EventConfiguration fields:
  - Title (required text field)
  - Description (optional text field)
  - Category (dropdown + "Manage Categories" button beside it)
  - IsEnabled (checkbox — saved as part of normal Save button flow on this screen)
  - Triggering Events (multi-value input — add/remove event type strings)
  - Secondary Events (multi-value input — add/remove event type strings)
  - Secondary Wait Time Ms (numeric input)
  - Prompt (multi-line text area)
  - Expected Results Schema (multi-line text area — user pastes/types JSON template)
  - Title Display Mode (dropdown: None / Display / Announce / Both)
  - Display Fields (multi-value input — keys from Expected Results Schema)
  - Display Keys (checkbox)
  - Announce Fields (multi-value input — keys from Expected Results Schema)
  - Announce Keys (checkbox)
  - Show Tray Notification (checkbox)
- Save button, Cancel button, Delete button (with confirmation)

### Category Management Screen
- Simple list of categories with Add, Rename, Delete
- Delete warns if category is in use by existing EventConfigurations
- Accessible from a button beside the Category dropdown on both the
  Selection Screen and the Edit Screen

### Settings Screen
- OpenAI API Key (masked input field)
- OpenAI Model (dropdown, populated from known models or user-entered)
- TTS Voice (dropdown, populated from installed Windows SAPI voices, with a "Test Voice"
  button beside it that speaks a sample phrase using the currently selected voice so the
  user can audition voices before saving)
- TTS Enabled (global toggle)
- Tray Notification Global Override (enable/disable all tray notifications)
- Theme (Light / Dark toggle)
- Always On Top (toggle)
- Save and Cancel buttons

### Test Screen
- Multi-line text area to paste one or more raw journal JSON lines
- "Run Test" button — processes pasted input through the full pipeline exactly
  as if lines came from the journal directory (secondary wait timer, OpenAI call,
  display output, TTS announce)
- Output area shows the same display as the main window response area
- Useful for building and validating new EventConfiguration entries

---

## Coding Conventions
- Use `async/await` throughout — no `.Result` or `.Wait()` calls
- Use CommunityToolkit.Mvvm source generators:
  `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]`
- ViewModels must have zero references to WPF UI types (no Window, UserControl, etc.)
- All service calls go through interfaces defined in EDJP.Core/Interfaces/
- Register all services in the DI container in App.xaml.cs at startup
- Use EF Core repository pattern — no raw SQL, no DbContext calls outside repositories
- Use `ILogger<T>` for all logging, routed to the flat file logger (EDAI.log)
- Null safety: enable nullable reference types (`<Nullable>enable</Nullable>` in csproj)
- One class per file. File name matches class name.

---

## Out of Scope (Do Not Implement)
- Course plotting or navigation assistance
- Exobiology tracking
- Fleet carrier management (future phase — EDJP.Data is structured to support it)
- Trade route optimization (future phase)
- Multi-commander configurations
- Cloud sync or remote access
- Any web or API server component