using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using EDAI.Core.Interfaces;
using EDAI.Core.Journal;
using EDAI.Core.Logging;
using EDAI.Core.Models;
using EDAI.Core.OpenAI;
using EDAI.Core.Pipeline;
using EDAI.Core.Scripting;
using EDAI.Core.TTS;
using EDAI.Data;
using EDAI.Data.Repositories;
using EDAI.UI.Services;
using EDAI.UI.ViewModels;
using EDAI.UI.Views;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDAI.UI;

public partial class App : Application
{
    private ServiceProvider? _services;
    private TrayIconService? _tray;
    private int? _currentSessionId;

    // Set before calling Shutdown() so MainWindow.OnClosing knows not to cancel.
    internal bool IsShuttingDown { get; private set; }

    internal void BeginShutdown()
    {
        IsShuttingDown = true;
        Shutdown();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var showSplash = TryApplyEarlyAppearance();

        SplashWindow? splash = null;
        Task splashMinimum = Task.CompletedTask;
        if (showSplash)
        {
            splash = new SplashWindow();
            CenterOnPrimaryMonitor(splash);
            splash.Show();
            splashMinimum = Task.Delay(5000);
        }

        _services = BuildServiceProvider();

        // Apply EF migrations
        var factory = _services.GetRequiredService<IDbContextFactory<EdaiDbContext>>();
        await using (var ctx = await factory.CreateDbContextAsync())
            await ctx.Database.MigrateAsync();

        // Start the global action queue (FIFO serialiser for display + TTS + logging)
        var actionQueue = _services.GetRequiredService<IActionQueue>();
        await actionQueue.StartAsync();

        // Apply saved settings
        var settingsRepo = _services.GetRequiredService<ISettingsRepository>();
        var settings = await settingsRepo.GetAsync();

        // Resolve journal path before starting the watcher.
        var journalPathOptions = _services.GetRequiredService<JournalPathOptions>();
        journalPathOptions.Path = settings.JournalPath;

        ApplyAppearance(settings);

        var scriptingService = _services.GetRequiredService<IScriptingService>();
        scriptingService.UpdatePermissions(new ScriptingPermissions
        {
            FileSystem       = settings.ScriptingAllowFileSystem,
            Network          = settings.ScriptingAllowNetwork,
            ProcessExecution = settings.ScriptingAllowProcessExecution,
            Reflection       = settings.ScriptingAllowReflection,
        });

        var composite = _services.GetRequiredService<CompositeTtsService>();
        composite.ActiveProvider = settings.TtsProvider;
        composite.IsEnabled      = settings.TtsEnabled;

        // Load Edge voices in background so startup isn't blocked by the network call.
        _ = composite.LoadEdgeVoicesAsync();

        // Apply saved voice / rate / pitch for the active engine.
        if (composite.ActiveProvider == CompositeTtsService.ProviderEdge)
        {
            composite.ConfigureEdge(
                settings.EdgeTtsVoice    ?? "en-US-AvaNeural",
                settings.EdgeTtsLanguage ?? "en-US",
                settings.EdgeTtsRate,
                settings.EdgeTtsPitch);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(settings.TtsVoiceName))
                composite.SetVoice(settings.TtsVoiceName);
        }

        // Start tray icon
        _tray = new TrayIconService();
        _tray.Show();

        // Wire tray notifications from OutputDispatcher
        var dispatcher = _services.GetRequiredService<IOutputDispatcher>();
        dispatcher.ResponseReceived += OnResponseReceived;

        // Wire journal → pipeline
        var watcher = _services.GetRequiredService<IJournalWatcher>();
        var parser = _services.GetRequiredService<IJournalParser>();
        var orchestrator = _services.GetRequiredService<IPipelineOrchestrator>();
        var mainVm = _services.GetRequiredService<MainWindowViewModel>();

        var sessionRepo = _services.GetRequiredService<ISessionHistoryRepository>();

        orchestrator.EventReceived += (_, e) => mainVm.NotifyJournalEvent(e.EventType);

        watcher.JournalLineReceived += async (_, args) =>
        {
            var parsed = parser.TryParse(args.Line.RawJson);
            if (parsed != null)
            {
                if (parsed.EventType == "LoadGame")
                    await HandleLoadGameAsync(parsed.RawJson, watcher, sessionRepo);

                await orchestrator.ProcessAsync(parsed);
            }
        };

        await watcher.StartAsync();

        var mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;

        // Size — use saved values or sensible defaults so centering is accurate.
        double winW = settings.WindowWidth  > 0 ? settings.WindowWidth  : 900;
        double winH = settings.WindowHeight > 0 ? settings.WindowHeight : 600;
        mainWindow.Width  = winW;
        mainWindow.Height = winH;

        // Start centered on the primary monitor; override with saved position if it is
        // still within the current virtual desktop bounds (guard against saved positions
        // from a monitor that is no longer connected).
        CenterOnPrimaryMonitor(mainWindow);
        if (settings.WindowLeft.HasValue && settings.WindowTop.HasValue)
        {
            double left = settings.WindowLeft.Value;
            double top  = settings.WindowTop.Value;
            double vLeft   = SystemParameters.VirtualScreenLeft;
            double vTop    = SystemParameters.VirtualScreenTop;
            double vRight  = vLeft + SystemParameters.VirtualScreenWidth;
            double vBottom = vTop  + SystemParameters.VirtualScreenHeight;

            // Require at least a 100×50 strip of the window to be on-screen.
            // Compute the actual pixel overlap between the window rect and the virtual desktop.
            double xVisible = Math.Min(left + winW, vRight)  - Math.Max(left, vLeft);
            double yVisible = Math.Min(top  + winH, vBottom) - Math.Max(top,  vTop);
            bool onScreen = xVisible >= 100 && yVisible >= 50;

            if (onScreen)
            {
                mainWindow.Left = left;
                mainWindow.Top  = top;
            }
        }

        mainWindow.Topmost = settings.AlwaysOnTop;
        if (settings.IsMaximized) mainWindow.WindowState = WindowState.Maximized;

        // Splash closes first, then main window appears.
        await splashMinimum;
        splash?.Close();
        // Must be OnExplicitShutdown: closing the main window hides it to tray
        // instead of exiting; only the tray "Exit" option calls BeginShutdown().
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        mainWindow.Show();
    }

    private async void OnResponseReceived(object? sender, AiResponseReceivedEventArgs e)
    {
        if (_tray == null) return;
        if (!e.ShowTrayNotification) return;

        // MainWindow is a WPF object — must check visibility on the UI thread.
        // IsVisible is false when the window has been hidden to the tray.
        bool isHidden = await Dispatcher.InvokeAsync(() => MainWindow?.IsVisible == false);
        if (!isHidden) return;

        var settingsRepo = _services!.GetRequiredService<ISettingsRepository>();
        var settings = await settingsRepo.GetAsync();
        if (!settings.TrayNotificationsEnabled) return;

        var summary = e.DisplayedOutput ?? e.AnnouncedOutput ?? string.Empty;
        if (summary.Length > 120) summary = summary[..120] + "…";
        _tray.ShowBalloon(e.ConfigTitle, summary);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_services != null)
        {
            // Stop watching first — no new events will enter the pipeline.
            var watcher = _services.GetService<IJournalWatcher>();
            if (watcher != null) await watcher.StopAsync();

            // Drain the action queue so any in-flight display/TTS/log work finishes
            // before we tear down dependencies (DB, TTS synth, session).
            var actionQueue = _services.GetService<IActionQueue>();
            if (actionQueue != null) await actionQueue.StopAsync();

            if (_currentSessionId.HasValue)
            {
                var sessionRepo = _services.GetService<ISessionHistoryRepository>();
                if (sessionRepo != null)
                    await sessionRepo.EndSessionAsync(_currentSessionId.Value);
            }

            _tray?.Dispose();
            await _services.DisposeAsync();
        }
        base.OnExit(e);
    }

    private async Task HandleLoadGameAsync(
        string rawJson, IJournalWatcher watcher, ISessionHistoryRepository sessionRepo)
    {
        // Close any previously open session from a prior run.
        if (_currentSessionId.HasValue)
            await sessionRepo.EndSessionAsync(_currentSessionId.Value);

        string commanderName = "Unknown";
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.TryGetProperty("Commander", out var prop))
                commanderName = prop.GetString() ?? commanderName;
        }
        catch { }

        var session = await sessionRepo.StartSessionAsync(
            commanderName, watcher.CurrentJournalFileName ?? string.Empty);
        _currentSessionId = session.Id;
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var sc = new ServiceCollection();

        var logPath = Path.Combine(AppContext.BaseDirectory, "EDAI.log");
        sc.AddLogging(b =>
        {
            b.AddProvider(new FileLoggerProvider(logPath));
            b.SetMinimumLevel(LogLevel.Information);
        });

        var dbPath = Path.Combine(AppContext.BaseDirectory, "EDAI.db");
        sc.AddDbContextFactory<EdaiDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}")
               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        // Repositories
        sc.AddSingleton<ISettingsRepository, SettingsRepository>();
        sc.AddSingleton<ICategoryRepository, CategoryRepository>();
        sc.AddSingleton<IEventConfigurationRepository, EventConfigurationRepository>();
        sc.AddSingleton<ISessionHistoryRepository, SessionHistoryRepository>();
        sc.AddSingleton<IResponseLogRepository, ResponseLogRepository>();
        sc.AddSingleton<IVoiceCacheRepository, VoiceCacheRepository>();

        // Core services
        sc.AddSingleton<IErrorService, ErrorService>();
        sc.AddSingleton<JournalPathOptions>();
        sc.AddSingleton<IJournalParser, JournalParser>();
        sc.AddSingleton<IJournalWatcher, JournalWatcher>();
        sc.AddSingleton<JournalAuxFileReader>();
        sc.AddSingleton<ISessionService, SessionService>();
        sc.AddSingleton<IJournalAuxFileReader>(sp =>
            new CompositeAuxReader(
                sp.GetRequiredService<JournalAuxFileReader>(),
                sp.GetRequiredService<ISessionService>()));
        sc.AddSingleton<ITriggerMatcher, TriggerMatcher>();
        sc.AddSingleton<IPromptBuilder, PromptBuilder>();
        sc.AddSingleton<IOpenAIService, OpenAIService>();
        sc.AddSingleton<IResponseParser, ResponseParser>();
        sc.AddSingleton<TtsService>();
        sc.AddSingleton<EdgeTtsService>();
        sc.AddSingleton<VoiceCacheService>(sp =>
        {
            var cacheDir = Path.Combine(AppContext.BaseDirectory, "voice_cache");
            return new VoiceCacheService(
                sp.GetRequiredService<IVoiceCacheRepository>(),
                cacheDir,
                sp.GetRequiredService<ILogger<VoiceCacheService>>());
        });
        sc.AddSingleton<CompositeTtsService>();
        sc.AddSingleton<ITtsService>(sp => sp.GetRequiredService<CompositeTtsService>());
        sc.AddSingleton<IActionQueue, ActionQueue>();
        sc.AddSingleton<IOutputDispatcher, OutputDispatcher>();
        sc.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
        sc.AddSingleton<IConfigExportService, ConfigExportService>();
        sc.AddSingleton<IScriptingService, ScriptingService>();

        // UI — navigation, ViewModels, Windows
        sc.AddSingleton<INavigationService, NavigationService>();
        sc.AddSingleton<IFileDialogService, FileDialogService>();
        sc.AddSingleton<MainWindowViewModel>();
        sc.AddSingleton<MainWindow>();

        sc.AddTransient<SettingsViewModel>();
        sc.AddTransient<SettingsWindow>();
        sc.AddTransient<CategoryManagementViewModel>();
        sc.AddTransient<CategoryManagementWindow>();
        sc.AddTransient<EventConfigEditViewModel>();
        sc.AddTransient<EventConfigEditWindow>();
        sc.AddTransient<ScriptDesignerViewModel>();
        sc.AddTransient<ScriptDesignerWindow>();
        sc.AddTransient<EventConfigSelectionViewModel>();
        sc.AddTransient<EventConfigSelectionWindow>();
        sc.AddTransient<TestViewModel>();
        sc.AddTransient<TestWindow>();
        sc.AddTransient<ThemeViewModel>();
        sc.AddTransient<ThemeWindow>();
        sc.AddTransient<AboutWindow>();
        sc.AddTransient<HistoryViewModel>();
        sc.AddTransient<HistoryWindow>();

        return sc.BuildServiceProvider();
    }

    private static void CenterOnPrimaryMonitor(Window w)
    {
        w.WindowStartupLocation = WindowStartupLocation.Manual;
        w.Left = (SystemParameters.PrimaryScreenWidth  - w.Width)  / 2;
        w.Top  = (SystemParameters.PrimaryScreenHeight - w.Height) / 2;
    }

    // Synchronous — must not yield before the splash window is shown so that no WPF
    // frame is ever rendered with the App.xaml orange defaults.
    // Raw SQL targets only stable columns so that missing pending-migration columns
    // don't prevent appearance loading.
    private static bool TryApplyEarlyAppearance()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "EDAI.db");
        if (!File.Exists(dbPath)) return true;
        try
        {
            var options = new DbContextOptionsBuilder<EdaiDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            using var ctx = new EdaiDbContext(options);
            ctx.Database.OpenConnection();
            var conn = ctx.Database.GetDbConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT Theme, PrimaryColor, CustomBackgroundColor, " +
                "CustomForegroundColor, FontFamily, FontSize " +
                "FROM Settings LIMIT 1";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return true;

            ApplyAppearance(new SettingsModel
            {
                Theme                 = reader.IsDBNull(0) ? "Dark"    : reader.GetString(0),
                PrimaryColor          = reader.IsDBNull(1) ? "#FF6D00" : reader.GetString(1),
                CustomBackgroundColor = reader.IsDBNull(2) ? null       : reader.GetString(2),
                CustomForegroundColor = reader.IsDBNull(3) ? null       : reader.GetString(3),
                FontFamily            = reader.IsDBNull(4) ? null       : reader.GetString(4),
                FontSize              = reader.IsDBNull(5) ? 14.0       : reader.GetDouble(5),
            });

            // ShowSplashScreen was added later — query separately; fall back to true if absent.
            try
            {
                using var splashCmd = conn.CreateCommand();
                splashCmd.CommandText = "SELECT ShowSplashScreen FROM Settings LIMIT 1";
                var result = splashCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result) != 0;
            }
            catch { }

            return true;
        }
        catch { return true; }
    }

    // Cached for font application to new windows opened after settings are saved.
    private static string? _currentFontFamily;
    private static double _currentFontSize = 14.0;

    internal static void ApplyAppearance(SettingsModel settings)
    {
        // ── Base theme + primary color ───────────────────────────────────────
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(string.Equals(settings.Theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? BaseTheme.Light
            : BaseTheme.Dark);
        var primaryColor = ParseColor(settings.PrimaryColor, Color.FromRgb(0xFF, 0x6D, 0x00));
        theme.SetPrimaryColor(primaryColor);
        paletteHelper.SetTheme(theme);

        // ── Custom background / foreground overrides ─────────────────────────
        var resources = Current.Resources;

        if (!string.IsNullOrEmpty(settings.CustomBackgroundColor))
            resources["MaterialDesign.Brush.Background"] =
                new SolidColorBrush(ParseColor(settings.CustomBackgroundColor, Color.FromRgb(18, 18, 18)));
        else
            resources.Remove("MaterialDesign.Brush.Background");

        if (!string.IsNullOrEmpty(settings.CustomForegroundColor))
        {
            var brush = new SolidColorBrush(ParseColor(settings.CustomForegroundColor, Colors.White));
            resources["MaterialDesign.Brush.Foreground"] = brush;
            resources["MaterialDesign.Brush.Body"]       = brush;
        }
        else
        {
            resources.Remove("MaterialDesign.Brush.Foreground");
            resources.Remove("MaterialDesign.Brush.Body");
        }

        // ── Toolbar / button custom colors ───────────────────────────────────
        // ToolbarBackground: custom color, or fall back to the primary accent color
        resources["EDAI.Brush.ToolbarBackground"] = !string.IsNullOrEmpty(settings.ToolbarBackground)
            ? new SolidColorBrush(ParseColor(settings.ToolbarBackground, primaryColor))
            : new SolidColorBrush(primaryColor);

        // ToolbarForeground: custom color, or fall back to whatever MD computed for primary
        var autoToolbarFg = resources["MaterialDesign.Brush.Primary.Foreground"] as System.Windows.Media.Brush
                            ?? new SolidColorBrush(Colors.White);
        resources["EDAI.Brush.ToolbarForeground"] = !string.IsNullOrEmpty(settings.ToolbarForeground)
            ? new SolidColorBrush(ParseColor(settings.ToolbarForeground, Colors.White))
            : autoToolbarFg;

        // ButtonBackground: custom color or fall back to primary accent (same as MDIX default)
        resources["EDAI.Brush.ButtonBackground"] = !string.IsNullOrEmpty(settings.ButtonBackground)
            ? new SolidColorBrush(ParseColor(settings.ButtonBackground, primaryColor))
            : new SolidColorBrush(primaryColor);

        // ButtonForeground: custom color or fall back to the auto-computed foreground for
        // the primary color (the accessible text color MDIX uses on primary-background buttons)
        var autoButtonFg = resources["MaterialDesign.Brush.Primary.Foreground"] as System.Windows.Media.Brush
                           ?? new SolidColorBrush(Colors.White);
        resources["EDAI.Brush.ButtonForeground"] = !string.IsNullOrEmpty(settings.ButtonForeground)
            ? new SolidColorBrush(ParseColor(settings.ButtonForeground, Colors.White))
            : autoButtonFg;

        // ControlBackground: overrides the card/surface background used by form controls
        if (!string.IsNullOrEmpty(settings.ControlBackground))
        {
            var brush = new SolidColorBrush(ParseColor(settings.ControlBackground, Color.FromRgb(30, 30, 30)));
            resources["EDAI.Brush.ControlBackground"] = brush;
            resources["MaterialDesign.Brush.Card.Background"] = brush;
        }
        else
        {
            resources.Remove("EDAI.Brush.ControlBackground");
            resources.Remove("MaterialDesign.Brush.Card.Background");
        }

        // ── Control Border (TextBox / ComboBox / CheckBox outline at rest) ─────
        // When no custom color: fall back to MDIX's current inactive-border colour so
        // the override brush stays visually neutral until the user picks a value.
        var mdixBorderFallback = GetMdixBrushColor(resources, "MaterialDesign.Brush.TextBox.OutlineInactiveBorder",
                                                    Color.FromRgb(97, 97, 97));
        var controlBorderColor = !string.IsNullOrEmpty(settings.ControlBorderColor)
            ? ParseColor(settings.ControlBorderColor, mdixBorderFallback)
            : mdixBorderFallback;
        resources["EDAI.Brush.ControlBorder"] = new SolidColorBrush(controlBorderColor);

        // ── Control Hover (TextBox / ComboBox border on mouse-over) ─────────
        var mdixHoverFallback = GetMdixBrushColor(resources, "MaterialDesign.Brush.TextBox.HoverBorder",
                                                   Color.FromRgb(144, 144, 144));
        var controlHoverColor = !string.IsNullOrEmpty(settings.ControlHoverBackground)
            ? ParseColor(settings.ControlHoverBackground, mdixHoverFallback)
            : mdixHoverFallback;
        resources["EDAI.Brush.ControlHover"] = new SolidColorBrush(controlHoverColor);

        // ── Font ─────────────────────────────────────────────────────────────
        _currentFontFamily = string.IsNullOrWhiteSpace(settings.FontFamily) ? null : settings.FontFamily;
        _currentFontSize   = settings.FontSize > 0 ? settings.FontSize : 14.0;

        foreach (Window w in Current.Windows)
            ApplyFontToWindow(w);

        // ── Script editor syntax colors ───────────────────────────────────────
        EDAI.UI.Services.ScriptEditorHighlighting.Apply(settings);
    }

    // Called by NavigationService when opening any new window.
    internal static void ApplyFontToWindow(Window w)
    {
        if (_currentFontFamily != null)
            TextElement.SetFontFamily(w, new FontFamily(_currentFontFamily));
        TextElement.SetFontSize(w, _currentFontSize);
    }

    // Looks up a brush colour from MDIX's merged ResourceDictionaries, ignoring
    // any override we may have placed directly in Application.Resources.
    private static Color GetMdixBrushColor(ResourceDictionary resources, string key, Color fallback)
    {
        foreach (ResourceDictionary rd in resources.MergedDictionaries)
        {
            if (rd[key] is SolidColorBrush b) return b.Color;
        }
        return fallback;
    }

    private static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { return (Color)ColorConverter.ConvertFromString(hex)!; }
        catch { return fallback; }
    }

    internal ServiceProvider Services => _services
        ?? throw new InvalidOperationException("ServiceProvider not yet initialised.");
}
