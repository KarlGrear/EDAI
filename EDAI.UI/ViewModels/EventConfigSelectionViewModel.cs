using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.UI.Services;

namespace EDAI.UI.ViewModels;

public sealed partial class EventConfigSelectionViewModel : ObservableObject
{
    private readonly IEventConfigurationRepository _repo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly INavigationService _navigation;
    private readonly IConfigExportService _exportService;
    private readonly IFileDialogService _fileDialogs;

    public ObservableCollection<EventConfigRowViewModel> Rows { get; } = [];
    public ICollectionView RowsView { get; }

    public ObservableCollection<CategoryModel> Categories { get; } = [];

    [ObservableProperty] private CategoryModel _selectedCategory = CategoryModel.All;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private EventConfigRowViewModel? _selectedRow;

    public EventConfigSelectionViewModel(
        IEventConfigurationRepository repo,
        ICategoryRepository categoryRepo,
        INavigationService navigation,
        IConfigExportService exportService,
        IFileDialogService fileDialogs)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _navigation = navigation;
        _exportService = exportService;
        _fileDialogs = fileDialogs;

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = ApplyFilter;
    }

    public async Task LoadAsync()
    {
        // Reset selection before clearing so the two-way ComboBox binding never
        // pushes null back through SelectedCategory while the collection is empty.
        SelectedCategory = CategoryModel.All;
        Rows.Clear();
        Categories.Clear();
        Categories.Add(CategoryModel.All);

        var cats = await _categoryRepo.GetAllAsync();
        foreach (var c in cats) Categories.Add(c);

        var configs = await _repo.GetAllAsync();
        foreach (var m in configs) Rows.Add(new EventConfigRowViewModel(m));
    }

    partial void OnSelectedCategoryChanged(CategoryModel value) => RowsView.Refresh();
    partial void OnSearchTextChanged(string value) => RowsView.Refresh();

    private bool ApplyFilter(object obj)
    {
        if (obj is not EventConfigRowViewModel row) return false;

        if (SelectedCategory != null && SelectedCategory != CategoryModel.All && row.CategoryName != SelectedCategory.Name)
            return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            if (!row.Title.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                !row.TriggeringEventsSummary.Contains(search, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task SaveEnabled(EventConfigRowViewModel row)
    {
        await _repo.SetEnabledAsync(row.Id, row.IsEnabled);
        row.IsPendingSave = false;
    }

    [RelayCommand]
    private void CancelEnabled(EventConfigRowViewModel row)
    {
        row.IsEnabled = !row.IsEnabled;
        row.IsPendingSave = false;
    }

    [RelayCommand]
    private void NewConfig() =>
        _navigation.ShowEventConfigEdit(null, async () => await LoadAsync());

    [RelayCommand]
    private void EditConfig(EventConfigRowViewModel? row)
    {
        if (row == null) return;
        _navigation.ShowEventConfigEdit(row.Id, async () => await LoadAsync());
    }

    [RelayCommand]
    private void TestConfig(EventConfigRowViewModel? row) =>
        _navigation.ShowTest(row?.Id);

    [RelayCommand]
    private async Task DeleteConfig(EventConfigRowViewModel? row)
    {
        if (row == null) return;
        var result = MessageBox.Show(
            $"Delete '{row.Title}'? This cannot be undone.",
            "Delete Configuration",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        await _repo.DeleteAsync(row.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DuplicateConfig(EventConfigRowViewModel? row)
    {
        if (row == null) return;
        var original = await _repo.GetByIdAsync(row.Id);
        if (original == null) return;

        var copy = new EventConfigurationModel
        {
            Title             = $"{original.Title} - Copy",
            Description       = original.Description,
            CategoryId        = original.CategoryId,
            IsEnabled         = original.IsEnabled,
            TriggeringEvents  = [.. original.TriggeringEvents],
            SecondaryEvents   = [.. original.SecondaryEvents],
            SecondaryWaitTimeMs = original.SecondaryWaitTimeMs,
            Prompt            = original.Prompt,
            ExpectedResultsSchema = original.ExpectedResultsSchema,
            DisplayTitle      = original.DisplayTitle,
            AnnounceTitle     = original.AnnounceTitle,
            DisplayFields     = [.. original.DisplayFields],
            DisplayKeys       = original.DisplayKeys,
            AnnounceFields    = [.. original.AnnounceFields],
            AnnounceKeys      = original.AnnounceKeys,
            ShowTrayNotification = original.ShowTrayNotification,
            SendToAi          = original.SendToAi,
            SendFullTriggerEvent = original.SendFullTriggerEvent,
            ModelOverride     = original.ModelOverride,
            TriggerCondition  = original.TriggerCondition,
            DisplayCondition  = original.DisplayCondition,
            AnnounceCondition = original.AnnounceCondition,
        };

        await _repo.AddAsync(copy);
        await LoadAsync();
    }

    [RelayCommand]
    private void ManageCategories() =>
        _navigation.ShowCategoryManagement(async () => await LoadAsync());

    [RelayCommand]
    private async Task ExportConfig(EventConfigRowViewModel? row)
    {
        if (row == null) return;

        var config = await _repo.GetByIdAsync(row.Id);
        if (config == null) return;

        var safeName = string.Concat(config.Title.Split(Path.GetInvalidFileNameChars()));
        var path = _fileDialogs.SaveFile(
            title: "Export Configuration",
            defaultFileName: $"{safeName}.edai.json",
            filter: "EDAI Configuration (*.edai.json)|*.edai.json|JSON files (*.json)|*.json|All files (*.*)|*.*");

        if (path == null) return;

        try
        {
            var json = _exportService.Serialize(config);
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Export failed:\n{ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ImportConfig()
    {
        var path = _fileDialogs.OpenFile(
            title: "Import Configuration",
            filter: "EDAI Configuration (*.edai.json)|*.edai.json|JSON files (*.json)|*.json|All files (*.*)|*.*");

        if (path == null) return;

        string json;
        try
        {
            json = await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not read file:\n{ex.Message}",
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var (valid, error, payload) = _exportService.Deserialize(json);
        if (!valid || payload == null)
        {
            MessageBox.Show(
                $"Invalid configuration file:\n{error}",
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        // Resolve category by name — create it if it doesn't exist yet.
        int? categoryId = null;
        if (!string.IsNullOrWhiteSpace(payload.CategoryName))
        {
            var cats = await _categoryRepo.GetAllAsync();
            var match = cats.FirstOrDefault(c =>
                string.Equals(c.Name, payload.CategoryName, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                categoryId = match.Id;
            }
            else
            {
                var created = await _categoryRepo.AddAsync(payload.CategoryName);
                categoryId = created.Id;
            }
        }

        var newConfig = new EventConfigurationModel
        {
            Title                = payload.Title,
            Description          = payload.Description,
            CategoryId           = categoryId,
            CategoryName         = payload.CategoryName,
            IsEnabled            = false, // require explicit enable after review
            TriggeringEvents     = payload.TriggeringEvents,
            TriggerCondition     = payload.TriggerCondition,
            SecondaryEvents      = payload.SecondaryEvents,
            SecondaryWaitTimeMs  = payload.SecondaryWaitTimeMs,
            SendToAi             = payload.SendToAi,
            SendFullTriggerEvent = payload.SendFullTriggerEvent,
            Prompt               = payload.Prompt,
            ExpectedResultsSchema = payload.ExpectedResultsSchema,
            ModelOverride        = payload.ModelOverride,
            DisplayTitle         = payload.DisplayTitle,
            DisplayFields        = payload.DisplayFields,
            DisplayKeys          = payload.DisplayKeys,
            DisplayCondition     = payload.DisplayCondition,
            AnnounceTitle        = payload.AnnounceTitle,
            AnnounceFields       = payload.AnnounceFields,
            AnnounceKeys         = payload.AnnounceKeys,
            AnnounceCondition    = payload.AnnounceCondition,
            ShowTrayNotification = payload.ShowTrayNotification,
        };

        await _repo.AddAsync(newConfig);
        await LoadAsync();

        MessageBox.Show(
            $"'{payload.Title}' imported successfully.\n\nThe configuration has been added as disabled — enable it when you're ready.",
            "Import Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
