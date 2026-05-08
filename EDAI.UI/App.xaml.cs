using System.IO;
using System.Windows;
using EDAI.Core.Interfaces;
using EDAI.Core.Journal;
using EDAI.Core.Logging;
using EDAI.Core.Models;
using EDAI.Core.OpenAI;
using EDAI.Core.Pipeline;
using EDAI.Core.TTS;
using EDAI.Data;
using EDAI.Data.Repositories;
using EDAI.UI.Services;
using EDAI.UI.ViewModels;
using EDAI.UI.Views;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDAI.UI;

public partial class App : Application
{
    private ServiceProvider? _services;
    private TrayIconService? _tray;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var splash = new SplashWindow();
        splash.Show();

        _services = BuildServiceProvider();

        // Apply EF migrations
        var factory = _services.GetRequiredService<IDbContextFactory<EdaiDbContext>>();
        await using (var ctx = await factory.CreateDbContextAsync())
            await ctx.Database.MigrateAsync();

        // Apply saved settings
        var settingsRepo = _services.GetRequiredService<ISettingsRepository>();
        var settings = await settingsRepo.GetAsync();

        ApplyTheme(settings.Theme);

        var tts = _services.GetRequiredService<ITtsService>();
        tts.IsEnabled = settings.TtsEnabled;
        if (!string.IsNullOrWhiteSpace(settings.TtsVoiceName))
            tts.SetVoice(settings.TtsVoiceName);

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

        watcher.JournalLineReceived += async (_, args) =>
        {
            var parsed = parser.TryParse(args.Line.RawJson);
            if (parsed != null)
            {
                mainVm.NotifyJournalEvent(parsed.EventType);
                await orchestrator.ProcessAsync(parsed);
            }
        };

        await watcher.StartAsync();

        var mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;

        if (settings.WindowWidth > 0) mainWindow.Width = settings.WindowWidth;
        if (settings.WindowHeight > 0) mainWindow.Height = settings.WindowHeight;
        if (settings.WindowLeft.HasValue) mainWindow.Left = settings.WindowLeft.Value;
        if (settings.WindowTop.HasValue) mainWindow.Top = settings.WindowTop.Value;
        mainWindow.Topmost = settings.AlwaysOnTop;

        mainWindow.Show();
        splash.Close();
    }

    private async void OnResponseReceived(object? sender, AiResponseReceivedEventArgs e)
    {
        if (_tray == null) return;

        // MainWindow is a WPF object — must check visibility on the UI thread.
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
            var watcher = _services.GetService<IJournalWatcher>();
            if (watcher != null) await watcher.StopAsync();

            _tray?.Dispose();
            await _services.DisposeAsync();
        }
        base.OnExit(e);
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
            opt.UseSqlite($"Data Source={dbPath}"));

        // Repositories
        sc.AddSingleton<ISettingsRepository, SettingsRepository>();
        sc.AddSingleton<ICategoryRepository, CategoryRepository>();
        sc.AddSingleton<IEventConfigurationRepository, EventConfigurationRepository>();
        sc.AddSingleton<ISessionHistoryRepository, SessionHistoryRepository>();
        sc.AddSingleton<IResponseLogRepository, ResponseLogRepository>();

        // Core services
        sc.AddSingleton<IErrorService, ErrorService>();
        sc.AddSingleton<IJournalParser, JournalParser>();
        sc.AddSingleton<IJournalWatcher, JournalWatcher>();
        sc.AddSingleton<ITriggerMatcher, TriggerMatcher>();
        sc.AddSingleton<IPromptBuilder, PromptBuilder>();
        sc.AddSingleton<IOpenAIService, OpenAIService>();
        sc.AddSingleton<IResponseParser, ResponseParser>();
        sc.AddSingleton<ITtsService, TtsService>();
        sc.AddSingleton<IOutputDispatcher, OutputDispatcher>();
        sc.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();

        // UI — navigation, ViewModels, Windows
        sc.AddSingleton<INavigationService, NavigationService>();
        sc.AddSingleton<MainWindowViewModel>();
        sc.AddSingleton<MainWindow>();

        sc.AddTransient<SettingsViewModel>();
        sc.AddTransient<SettingsWindow>();
        sc.AddTransient<CategoryManagementViewModel>();
        sc.AddTransient<CategoryManagementWindow>();
        sc.AddTransient<EventConfigEditViewModel>();
        sc.AddTransient<EventConfigEditWindow>();
        sc.AddTransient<EventConfigSelectionViewModel>();
        sc.AddTransient<EventConfigSelectionWindow>();
        sc.AddTransient<TestViewModel>();
        sc.AddTransient<TestWindow>();

        return sc.BuildServiceProvider();
    }

    internal static void ApplyTheme(string theme)
    {
        var paletteHelper = new PaletteHelper();
        var currentTheme = paletteHelper.GetTheme();
        currentTheme.SetBaseTheme(string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? BaseTheme.Light
            : BaseTheme.Dark);
        paletteHelper.SetTheme(currentTheme);
    }

    internal ServiceProvider Services => _services
        ?? throw new InvalidOperationException("ServiceProvider not yet initialised.");
}
