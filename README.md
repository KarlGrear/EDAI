# Elite Dangerous AI (EDAI)

A Windows desktop AI co-pilot for Elite Dangerous commanders. EDAI watches your journal files in real time, routes configured events through OpenAI, reads responses aloud via Windows Text-to-Speech, and displays them in a compact overlay window.

---

## Features

- **Real-time journal monitoring** — tails the active journal file using `FileSystemWatcher`; no polling, no replaying old events
- **Token-conscious by design** — only events you explicitly configure ever reach OpenAI; all other events are silently discarded
- **Per-event AI pipelines** — each configuration has its own prompt, expected JSON schema, display rules, TTS rules, and optional trigger/display/announce conditions
- **Template engine** — embed live game data in prompts and output fields using `|trigger.*|`, `|result.*|`, and `|aux.*|` tokens
- **Auxiliary file access** — read Status.json, Market.json, NavRoute.json and five other Elite Dangerous companion files directly into prompts at runtime
- **Condition evaluator** — gate triggers, display, and speech on dynamic template expressions (`|trigger.StarSystem| == "Sol"`)
- **Secondary event collection** — wait a configurable window after a trigger to gather related follow-up events before building the prompt (e.g. collect `ScanDetailed` entries after `FSSAllBodiesFound`)
- **Per-pipeline queuing** — if the same trigger fires while its pipeline is already running, the new event is queued and processed in order
- **Local-only configs** — disable the AI call (`SendToAi = false`) to use the template/condition system for purely local display and speech
- **Per-config model override** — use a different OpenAI model (e.g. a faster/cheaper one) for specific configurations
- **Windows Text-to-Speech** — built-in SAPI voices, no internet required for speech; runs on a background thread so it never blocks the UI
- **Minimize to system tray** — journal watching and the AI pipeline continue running when the window is minimised; right-click the tray icon to restore or exit
- **Windows tray notifications** — optional balloon notifications when a response fires while minimised, with per-config and global overrides
- **Full theme customization** — HSV color picker for accent, background, text, toolbar, and button colors; Light/Dark base theme; font family and size
- **Encrypted API key storage** — your OpenAI key is stored using Windows DPAPI, never in plain text
- **SQLite persistence** — all settings, configurations, categories, session history, and response logs stored locally
- **Test screen** — paste raw journal JSON and run it through the full pipeline without needing the game running; separate template tester tab for building expressions

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
2. Click the **Settings** (cog ⚙) icon in the toolbar.
3. Paste your **OpenAI API Key**. Choose a model (`gpt-4o` or `gpt-5` recommended).
4. Optionally set your TTS voice, font, and notification preferences.
5. Click **Save**.
6. To customise colors, click the **Palette** (🎨) icon in the toolbar.

---

## Application Windows

### Main Window

The main window is always visible while EDAI is running (or in the system tray).

| Area | Description |
|---|---|
| **AI Responses** panel | Scrolling list of responses from all active pipelines, newest first |
| **Event Log** panel | Timestamped list of every journal event type received |
| **Status bar** | Last event received; error messages on the right |
| **Toolbar** | Navigation, quick toggles, and utility buttons |

Toolbar buttons (left to right):

| Icon | Action |
|---|---|
| ⚙ Cog | Application Settings |
| ☰ List | Event Configurations |
| 🧪 Test Tube | Test Screen |
| 📌 Pin | Toggle Always on Top |
| 🔊 Speaker | Toggle TTS on/off |
| 🎨 Palette | Theme Customisation |
| 🗑 Sweep | Clear response panel |
| ℹ Info | About |

### Settings Window

| Section | Options |
|---|---|
| **OpenAI** | API key (DPAPI-encrypted), model selection (gpt-5, gpt-5-mini, gpt-5-nano, gpt-4o, gpt-4o-mini, gpt-4-turbo, gpt-4, gpt-3.5-turbo, or any custom model ID) |
| **Text-to-Speech** | Voice selection with test button, global TTS on/off |
| **Notifications** | Global tray notification override |
| **Interface** | Always on Top, Show Splash Screen |
| **Font** | Font family (all system fonts), font size slider (8–32 pt) |

### Theme Window

Opened via the Palette icon. Changes apply in real time so you can preview before saving. Select an element from the dropdown, adjust the color, then click **Save** to persist.

| Element | Default | Notes |
|---|---|---|
| Accent Color | `#FF6D00` (ED orange) | Sets the Material Design primary palette |
| App Background | Dark theme default | Custom overrides the theme background |
| App Text | Dark theme default | Custom overrides the theme foreground |
| Toolbar Background | Accent color | Independent from accent — the toolbar strip |
| Toolbar Text | Auto-computed | Foreground for all toolbar text and icons |
| Button Text | Theme default | Optional override for button foreground |
| Event Background | Dark surface | Background of event/response cards in the panel |
| Control Border | `#606060` | Border color of text boxes and combo boxes |
| Control Hover | `#909090` | Hover/focus highlight on input controls |

The color picker uses HSV (hue/saturation/value) with RGB numeric inputs and a hex field. An eyedropper tool lets you sample any color from any pixel on screen. Click **Reset to Default** to remove the custom override for the selected element.

### Event Configurations Window

A filterable list of all event configurations with an inline **Enabled** toggle per row.

- **Category filter** — select a category or "(All)"
- **Search** — matches title or triggering events (partial, case-insensitive)
- **Enabled toggle** — toggling reveals inline Save ✔ / Cancel ✖ buttons so you can confirm or revert
- **Toolbar actions** — New, Edit, Duplicate, Test (opens Test screen pre-loaded), Delete
- **Double-click** any row to edit

### Event Configuration Edit Window

A four-tab form for a single pipeline configuration. Every field has an inline **ⓘ help icon**. See [Event Configuration Fields](#event-configuration-fields) below for full details.

| Tab | Contents |
|---|---|
| **General** | Title, Description, Category, Enabled, Model Override |
| **Trigger** | Send to AI flag, Send Full Trigger Event flag, Triggering Events, Trigger Condition, Secondary Events, Secondary Wait (ms) |
| **Processing** | Prompt, Expected Results Schema, Available Tokens hint panel |
| **Action** | Display (title, fields, keys, condition) and Announce (title, fields, keys, condition) side-by-side, Show Tray Notification |

The **Processing** tab is disabled (with an info banner) when **Send to AI** is unchecked on the Trigger tab.

### Test Window

Two tabs:

**Pipeline Test** — paste one or more raw journal JSON lines, optionally pick a specific configuration, and click **Run Test**. The event is routed through the full pipeline (secondary collection, OpenAI call, template engine, conditions) exactly as if it came from the journal directory. Results appear in the same response panel format as the main window.

**Template Tester** — paste trigger JSON and result JSON, type a template expression (e.g. `|trigger.StarSystem| is |result.star_class|`), and optionally a condition expression. Click **Evaluate** to see the resolved output and condition result without running a full pipeline call.

### Category Management Window

Add, rename, and delete categories. Deleting a category that is still referenced by one or more configurations shows a warning before proceeding.

---

## Event Configuration Fields

Each configuration represents one AI pipeline. Fields are grouped by function.

### Identity

| Field | Description |
|---|---|
| **Title** | Short name shown in the response panel header |
| **Description** | Optional notes for your own reference |
| **Category** | Optional grouping for the selection list filter |
| **Enabled** | Master on/off switch for this pipeline |

### Triggering

| Field | Description |
|---|---|
| **Triggering Events** | One or more journal event type names (OR logic). The pipeline fires when any of these event types is received, e.g. `FSDJump`, `DockingGranted`. |
| **Trigger Condition** | Optional template expression evaluated after the event type matches. If the expression resolves to false, the pipeline is skipped entirely. Example: `|trigger.StarSystem| != "Sol"` |

### Secondary Event Collection

| Field | Description |
|---|---|
| **Secondary Events** | Event types to collect after the trigger fires, e.g. `Scan`, `ScanDetailed` |
| **Secondary Wait (ms)** | How long to wait collecting secondary events before proceeding (default 1000 ms) |

Collected secondary events are included in the prompt as additional context JSON.

### AI Behaviour

| Field | Description |
|---|---|
| **Send to AI** | When unchecked, the OpenAI call is skipped entirely. The template engine and conditions still run, so you can use EDAI purely for local display or speech based on journal data. When unchecked, the Processing tab is disabled in the edit window. |
| **Prompt** | The instruction sent to OpenAI after the ship-AI system persona. Supports `|trigger.*|` and `|aux.*|` template tokens. |
| **Expected Results Schema** | JSON template defining the exact shape of the AI response. This is embedded in the prompt and controls which `|result.*|` tokens become available. Example: `{"star_class": "", "threat_level": "", "recommendation": ""}` Must be valid JSON — the edit form validates it on save. |
| **Model Override** | Use a different OpenAI model for this configuration only. Leave blank to use the global default from Settings. Available options: `gpt-5`, `gpt-5-mini`, `gpt-5-nano`, `gpt-4o`, `gpt-4o-mini`, `gpt-4-turbo`, `gpt-4`, `gpt-3.5-turbo`, or any custom model ID. |
| **Send Full Trigger Event** | Include the complete raw trigger event JSON in the prompt. Uncheck to reduce token usage when you only need specific fields via `|trigger.*|` tokens in the prompt. |

### Display (UI Response Panel)

| Field | Description |
|---|---|
| **Display Title** | Show the configuration title above the response in the panel |
| **Display Fields** | Template strings rendered in the panel, one per line. Each can be plain text or contain `|result.*|` and `|trigger.*|` tokens. Example: `System: |result.star_class| — |result.recommendation|` |
| **Display Keys** | Prefix each displayed value with its field name |
| **Display Condition** | Template expression. If it resolves to false, nothing is shown in the panel for this response. |

### Announce (Text-to-Speech)

| Field | Description |
|---|---|
| **Announce Title** | Prepend the configuration title to the spoken output |
| **Announce Fields** | Template strings sent to TTS, same token syntax as Display Fields |
| **Announce Keys** | Speak field names alongside values |
| **Announce Condition** | Template expression. If it resolves to false, nothing is spoken for this response. |
| **Show Tray Notification** | Show a Windows balloon notification when the app is minimised to the tray and this configuration fires. Subject to the global tray notification override in Settings. |

---

## Template Engine

Template tokens use the `|...|` syntax and are resolved against JSON data at runtime. Tokens can appear in the **Prompt**, **Display Fields**, **Announce Fields**, **Trigger Condition**, **Display Condition**, and **Announce Condition** fields.

### Token Prefixes

| Prefix | Data Source |
|---|---|
| `trigger.` | The triggering journal event JSON |
| `result.` | The AI response JSON (available after OpenAI call) |
| `status.` | Status.json — current ship state |
| `market.` | Market.json — local station market |
| `navroute.` | NavRoute.json — plotted navigation route |
| `outfitting.` | Outfitting.json — station outfitting options |
| `shiplocker.` | ShipLocker.json — cargo/inventory |
| `shipyard.` | Shipyard.json — station shipyard |
| `modulesinfo.` | ModulesInfo.json — installed ship modules |

Auxiliary files (`status.`, `market.`, etc.) are read fresh from your Elite Dangerous saved games folder on every pipeline run, so they reflect current game state at the moment the event fires.

### Token Syntax

Tokens use RFC 9535 JSONPath after the prefix:

```
|trigger.StarSystem|                          → string field from event JSON
|trigger.Factions[0].Name|                   → array element field
|result.threat_level|                        → field from AI response
|status.FireGroup|                           → field from Status.json
|navroute.Route[-1].StarSystem|              → last element of array
|count(trigger.Factions)|                   → count array elements
|count(trigger.Factions[?@.Allegiance=="Federation"])|  → count with filter
```

If a token cannot be resolved (missing field, file not available) it is left as-is in the output.

### Prompt Example

```
Analyse the system we just jumped to.
The system is |trigger.StarSystem| in |trigger.StarPos|.
We currently have |status.Fuel.FuelMain| tonnes of fuel.
Respond with a threat assessment and navigation recommendation.
```

### Display Field Example

```
⚠ |result.threat_level| — |result.recommendation|
```

---

## Condition Evaluator

Conditions are template expressions that resolve to true or false. A blank condition always passes (true). Conditions support standard comparison and logical operators:

| Operator | Meaning |
|---|---|
| `==` | Equal |
| `!=` | Not equal |
| `>` `>=` `<` `<=` | Numeric or lexicographic comparison |
| `&&` | AND (higher precedence than OR) |
| `\|\|` | OR |

Values can be quoted strings (`"Sol"`), numeric literals (`7`), booleans (`true`/`false`), or bare template tokens.

### Examples

```
|trigger.StarSystem| != "Sol"
```
```
|trigger.JumpDist| >= 20 && |status.Fuel.FuelMain| < 4
```
```
|result.threat_level| == "High" || |result.threat_level| == "Critical"
```
```
|count(trigger.Factions)| > 3
```

---

## Auxiliary File System

EDAI can read seven Elite Dangerous companion files from your saved games folder at the moment each pipeline runs:

| Token Prefix | File | Typical Contents |
|---|---|---|
| `status` | Status.json | Ship flags, fuel, fire group, cargo, legal status |
| `market` | Market.json | Commodity prices at the docked station |
| `navroute` | NavRoute.json | Full plotted route waypoints |
| `outfitting` | Outfitting.json | Modules available at current station |
| `shiplocker` | ShipLocker.json | Odyssey backpack/ship locker contents |
| `shipyard` | Shipyard.json | Ships available at current station |
| `modulesinfo` | ModulesInfo.json | Installed modules on current ship |

Files are opened with `FileShare.ReadWrite` so Elite Dangerous does not need to be closed. If a file is unavailable, the token is left unresolved without causing an error.

The **Template Tester** in the Test screen shows the list of known aux file identifiers and lets you verify token resolution before saving a configuration.

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
  TriggerCondition        – optional template/condition evaluated per config; skips if false
       │
       ▼
  ConfigPipeline          – per-config Channel queue; one AI call active at a time
       │
       ▼
  SecondaryEventCollector – waits SecondaryWaitTimeMs, collecting secondary events
       │
       ▼
  PromptBuilder           – resolves |trigger.*| and |aux.*| tokens; assembles:
       │                    system persona + config prompt + event JSON + schema
       ▼
  OpenAIService           – stateless chat completion in JSON-object mode
       │                    (skipped if SendToAi = false)
       │  raw JSON response
       ▼
  ResponseParser          – extracts fields, applies |result.*| and |trigger.*| templates
       │                    to DisplayFields and AnnounceFields; evaluates conditions
       │  AiResponse
       ▼
  OutputDispatcher        – raises ResponseReceived, enqueues TTS, writes ResponseLog
       ├──▶  UI response panel   (MainWindowViewModel)
       ├──▶  TtsService          (background speech queue)
       ├──▶  TrayIconService     (balloon notification if minimised)
       └──▶  ResponseLog         (SQLite — every response persisted)
```

Events that do not match any enabled configuration are discarded after the parser step — they never reach OpenAI and incur no cost.

---

## Example Configurations

### FSD Jump Analysis

Fires when you jump to a new system. Asks the AI to summarise the arrival and note any threats.

| Field | Value |
|---|---|
| Title | Jump Analysis |
| Triggering Events | `FSDJump` |
| Prompt | `Summarise this FSD jump. Note the star class, notable bodies if mentioned, and any potential threats. System: |trigger.StarSystem|. Fuel remaining: |status.Fuel.FuelMain| tonnes.` |
| Expected Results Schema | `{"star_class": "", "bodies_summary": "", "threat_level": "", "recommendation": ""}` |
| Display Fields | `|result.star_class| · |result.threat_level|`, `|result.recommendation|` |
| Announce Fields | `Jump complete. |result.star_class| star. |result.recommendation|` |

### Docking Notification (No AI)

Announces docking without making any AI call.

| Field | Value |
|---|---|
| Title | Docking |
| Triggering Events | `Docked` |
| Send to AI | ☐ unchecked |
| Announce Fields | `Docking confirmed at |trigger.StationName| in |trigger.StarSystem|.` |

### High-Population System Filter

Fires on jump only when the destination has a significant population.

| Field | Value |
|---|---|
| Triggering Events | `FSDJump` |
| Trigger Condition | `|trigger.Population| > 1000000` |
| Prompt | `This is a densely populated system. Assess political stability…` |

### Scan with Secondary Context

Fires on `FSSAllBodiesFound` and waits 3 seconds to collect any `Scan` events that follow.

| Field | Value |
|---|---|
| Triggering Events | `FSSAllBodiesFound` |
| Secondary Events | `Scan` |
| Secondary Wait (ms) | `3000` |
| Prompt | `Analyse the system scan results. The system contains the following bodies:` |

---

## Solution Structure

```
EliteDangerousAI.sln
├── EDAI.Core/               Business logic, services, models, interfaces
│   ├── Journal/             FileSystemWatcher, tail reader, parser, aux file reader
│   ├── OpenAI/              OpenAI client, response parser
│   ├── TTS/                 TTS service wrapper (System.Speech.Synthesis)
│   ├── Pipeline/            Trigger matching, secondary collection, template engine,
│   │                        condition evaluator, orchestrator, output dispatcher
│   ├── Logging/             File logger provider, error service
│   ├── Models/              Shared data models (EventConfigurationModel, SettingsModel, …)
│   └── Interfaces/          All service contracts
├── EDAI.UI/                 WPF application
│   ├── Controls/            HsvColorPicker, HelpIcon user controls
│   ├── Converters/          XAML value converters
│   ├── Services/            NavigationService, TrayIconService
│   ├── Validators/          Data annotation validators (JsonValidator)
│   ├── ViewModels/          MVVM ViewModels (CommunityToolkit.Mvvm source generators)
│   └── Views/               XAML windows (Main, Settings, Theme, EventConfig, Test, …)
└── EDAI.Data/               EF Core + SQLite data layer
    ├── Entities/            EF Core entity classes
    ├── Repositories/        Repository pattern implementations
    └── Migrations/          EF Core migrations (auto-applied on startup)
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
| JSONPath | RFC 9535 compliant library for template token resolution |

---

## Data Files

Both files are created next to the application executable on first run:

| File | Purpose |
|---|---|
| `EDAI.db` | SQLite database — settings, event configs, categories, session history, response logs |
| `EDAI.log` | Flat-file log — `[timestamp] [LEVEL] [source] message` |

### Database Tables

| Table | Contents |
|---|---|
| `Settings` | Singleton row: all application settings including API key (encrypted), theme, fonts, window position |
| `Categories` | User-defined categories for organising event configurations |
| `EventConfigurations` | All pipeline configurations with every field |
| `SessionHistory` | One record per play session (detected via `LoadGame` journal event) |
| `ResponseLogs` | One record per AI response: full prompt sent, raw AI reply, formatted output |

---

## Privacy

- Your OpenAI API key is encrypted with Windows DPAPI (bound to your Windows user account) before being stored in `EDAI.db`. It is never written in plain text anywhere.
- Journal event data is sent to OpenAI **only** for events you have explicitly configured. All other journal lines are parsed locally and immediately discarded.
- Auxiliary game files (Status.json, Market.json, etc.) are read locally only — they are never transmitted anywhere except as part of an OpenAI prompt you have explicitly configured.
- No telemetry, analytics, or external communication of any kind beyond the configured OpenAI API calls.

---

## License

MIT — see [LICENSE](LICENSE) for details.
