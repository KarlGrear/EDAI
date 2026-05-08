# Elite Dangerous AI (EDAI)

A Windows desktop AI co-pilot for Elite Dangerous commanders. EDAI watches your journal files in real time, routes configured events through OpenAI, reads responses aloud via Windows Text-to-Speech, and displays them in a compact overlay window.

---

## Features

- **Real-time journal monitoring** — tails the active journal file using `FileSystemWatcher`; no polling, no replaying old events
- **Token-conscious by design** — only events you explicitly configure ever reach OpenAI; all other events are silently discarded
- **Per-event AI pipelines** — each configuration has its own prompt, expected JSON schema, display rules, and TTS rules
- **Secondary event collection** — wait a configurable window after a trigger to gather related follow-up events before building the prompt (e.g. collect `ScanDetailed` entries after `FSSAllBodiesFound`)
- **Per-pipeline queuing** — if the same trigger fires while its pipeline is running, the new trigger is queued and processed in order
- **Windows Text-to-Speech** — built-in SAPI voices, no internet required for speech; runs on a background thread so it never blocks
- **Minimize to system tray** — journal watching and the AI pipeline continue running when the window is minimised
- **Windows tray notifications** — optional balloon notifications when a response fires while minimised
- **Light/Dark Material Design UI** — Elite Dangerous orange accent, switchable theme
- **Encrypted API key storage** — your OpenAI key is stored using Windows DPAPI, never in plain text
- **SQLite persistence** — all settings, configurations, categories, session history, and response logs stored locally

---

## Prerequisites

| Requirement | Version |
|---|---|
| Windows | 10 or 11 |
| .NET Runtime | 10.0 (LTS) |
| Elite Dangerous | Any (live journal files required) |
| OpenAI account | API key from [platform.openai.com](https://platform.openai.com) |

---

## Building from Source

```powershell
# Clone
git clone https://github.com/kpgrear/EliteDangerousAI.git
cd EliteDangerousAI

# Restore and build
dotnet build

# Run
dotnet run --project EDAI.UI
```

The first run automatically creates `EDAI.db` and `EDAI.log` next to the executable and applies all EF Core migrations.

---

## First-Run Setup

1. Launch the application — a splash screen appears while the database initialises.
2. Click the **Settings** (cog) icon in the toolbar.
3. Paste your **OpenAI API Key**. Choose a model (`gpt-4o` recommended).
4. Optionally configure your TTS voice and theme.
5. Click **Save**.

---

## Creating Your First Event Configuration

Event configurations tell EDAI what to do when a specific journal event is received.

1. Click the **Event Configurations** (list) icon in the toolbar.
2. Click **New**.
3. Fill in the form — every field has an inline **ⓘ help icon** you can click for an explanation:
   - **Title** — a short name shown in the response panel (e.g. *Jump Analysis*)
   - **Triggering Events** — journal event type(s) that activate this pipeline (e.g. `FSDJump`)
   - **Prompt** — the instruction sent to OpenAI after the ship-AI persona
   - **Expected Results Schema** — JSON template for the AI response shape, e.g.:
     ```json
     {"system_name": "", "star_class": "", "threat_level": "", "recommendation": ""}
     ```
   - **Display Fields** — keys whose values appear in the response panel
   - **Announce Fields** — keys whose values are spoken aloud
4. Check **Enabled** and click **Save**.

The next time the matching event fires in Elite Dangerous, EDAI sends it to OpenAI and displays and speaks the response.

---

## Pipeline Architecture

```
Journal file (live)
       │  FileSystemWatcher
       ▼
  JournalWatcher          – tails the active journal log, emits complete lines
       │
       ▼
  JournalParser           – extracts "event" field from each JSON line
       │  ParsedJournalEvent
       ▼
  TriggerMatcher          – finds enabled configs whose TriggeringEvents match (OR logic)
       │  per-config match list
       ▼
  ConfigPipeline          – per-config Channel queue; one AI call active at a time
       │
       ▼
  SecondaryEventCollector – waits SecondaryWaitTimeMs, collecting secondary events
       │
       ▼
  PromptBuilder           – assembles: ship persona + config prompt + event JSON + schema
       │  built prompt
       ▼
  OpenAIService           – stateless chat completion in JSON-object mode
       │  raw JSON response
       ▼
  ResponseParser          – extracts DisplayFields and AnnounceFields from the response
       │  AiResponse
       ▼
  OutputDispatcher        – raises ResponseReceived, enqueues TTS, writes ResponseLog
       ├──▶  UI response panel   (MainWindowViewModel)
       ├──▶  TtsService          (background speech queue)
       ├──▶  TrayIconService     (balloon notification if minimised)
       └──▶  ResponseLog         (SQLite — every response persisted)
```

---

## Solution Structure

```
EliteDangerousAI.sln
├── EDAI.Core/               Business logic, services, models, interfaces
│   ├── Journal/             FileSystemWatcher, tail reader, event parser
│   ├── OpenAI/              OpenAI client, response parser
│   ├── TTS/                 TTS service wrapper
│   ├── Pipeline/            Trigger matching, secondary collection, orchestrator
│   ├── Logging/             File logger provider, error service
│   ├── Models/              Shared data models and event args
│   └── Interfaces/          All service contracts (interfaces)
├── EDAI.UI/                 WPF application
│   ├── Controls/            Reusable UserControls (HelpIcon)
│   ├── Converters/          XAML value converters
│   ├── Services/            Navigation service, tray icon service
│   ├── ViewModels/          MVVM ViewModels (CommunityToolkit.Mvvm)
│   └── Views/               XAML windows
└── EDAI.Data/               EF Core + SQLite data layer
    ├── Entities/            EF Core entity classes
    ├── Repositories/        Repository pattern implementations
    └── Migrations/          EF Core migrations
```

---

## Technology Stack

| Layer | Technology |
|---|---|
| Language | C# / .NET 10 |
| UI | WPF + MVVM (CommunityToolkit.Mvvm source generators) |
| UI Library | Material Design in XAML (MaterialDesignThemes 5.x) |
| AI | OpenAI .NET SDK v2 |
| TTS | System.Speech.Synthesis (Windows SAPI, built-in) |
| Database | SQLite via EF Core 10 |
| Secrets | Windows DPAPI (`ProtectedData.CurrentUser`) |
| DI Container | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging → flat file (EDAI.log) |

---

## Data Files

Both files are created next to the application executable on first run:

| File | Purpose |
|---|---|
| `EDAI.db` | SQLite database — settings, event configs, session history, response logs |
| `EDAI.log` | Flat-file log — `[timestamp] [LEVEL] [source] message` |

---

## Privacy

- Your OpenAI API key is encrypted with Windows DPAPI (bound to your Windows user account) before being stored in `EDAI.db`. It is never written in plain text.
- Journal event data is sent to OpenAI **only** for events you have explicitly configured. All other journal lines are parsed locally and immediately discarded.
- No telemetry, analytics, or external communication of any kind beyond the configured OpenAI API calls.

---

## License

MIT — see [LICENSE](LICENSE) for details.
