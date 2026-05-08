using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.UI.Services;

namespace EDAI.UI.ViewModels;

public sealed partial class EventConfigEditViewModel : ObservableObject
{
    private readonly IEventConfigurationRepository _repo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly INavigationService _navigation;

    private int _id;

    public ObservableCollection<CategoryModel?> Categories { get; } = [];
    public ObservableCollection<string> TitleDisplayModes { get; } =
    [
        "None", "Display", "Announce", "Both"
    ];

    // Core fields
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private CategoryModel? _selectedCategory;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _prompt = string.Empty;
    [ObservableProperty] private string _expectedResultsSchema = string.Empty;
    [ObservableProperty] private string _selectedTitleDisplayMode = "None";
    [ObservableProperty] private bool _displayKeys;
    [ObservableProperty] private bool _announceKeys;
    [ObservableProperty] private bool _showTrayNotification;
    [ObservableProperty] private int _secondaryWaitTimeMs = 1000;

    // Multi-value collections
    public ObservableCollection<string> TriggeringEvents { get; } = [];
    public ObservableCollection<string> SecondaryEvents { get; } = [];
    public ObservableCollection<string> DisplayFields { get; } = [];
    public ObservableCollection<string> AnnounceFields { get; } = [];

    // New-item inputs
    [ObservableProperty] private string _newTriggeringEvent = string.Empty;
    [ObservableProperty] private string _newSecondaryEvent = string.Empty;
    [ObservableProperty] private string _newDisplayField = string.Empty;
    [ObservableProperty] private string _newAnnounceField = string.Empty;

    [ObservableProperty] private string? _validationError;
    public event EventHandler? CloseRequested;

    public bool IsEditMode => _id > 0;

    public EventConfigEditViewModel(
        IEventConfigurationRepository repo,
        ICategoryRepository categoryRepo,
        INavigationService navigation)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _navigation = navigation;
    }

    public async Task LoadAsync(int? configId = null)
    {
        Categories.Clear();
        Categories.Add(null);
        var cats = await _categoryRepo.GetAllAsync();
        foreach (var c in cats) Categories.Add(c);

        if (configId.HasValue && configId.Value > 0)
        {
            var model = await _repo.GetByIdAsync(configId.Value);
            if (model != null) PopulateFromModel(model);
        }
    }

    private void PopulateFromModel(EventConfigurationModel m)
    {
        _id = m.Id;
        Title = m.Title;
        Description = m.Description ?? string.Empty;
        SelectedCategory = Categories.FirstOrDefault(c => c?.Id == m.CategoryId);
        IsEnabled = m.IsEnabled;
        Prompt = m.Prompt;
        ExpectedResultsSchema = m.ExpectedResultsSchema ?? string.Empty;
        SelectedTitleDisplayMode = m.TitleDisplayMode.ToString();
        DisplayKeys = m.DisplayKeys;
        AnnounceKeys = m.AnnounceKeys;
        ShowTrayNotification = m.ShowTrayNotification;
        SecondaryWaitTimeMs = m.SecondaryWaitTimeMs;

        foreach (var e in m.TriggeringEvents) TriggeringEvents.Add(e);
        foreach (var e in m.SecondaryEvents) SecondaryEvents.Add(e);
        foreach (var f in m.DisplayFields) DisplayFields.Add(f);
        foreach (var f in m.AnnounceFields) AnnounceFields.Add(f);
    }

    // Add/Remove commands for multi-value lists
    [RelayCommand]
    private void AddTriggeringEvent()
    {
        var v = NewTriggeringEvent.Trim();
        if (!string.IsNullOrEmpty(v) && !TriggeringEvents.Contains(v)) { TriggeringEvents.Add(v); NewTriggeringEvent = string.Empty; }
    }

    [RelayCommand]
    private void AddSecondaryEvent()
    {
        var v = NewSecondaryEvent.Trim();
        if (!string.IsNullOrEmpty(v) && !SecondaryEvents.Contains(v)) { SecondaryEvents.Add(v); NewSecondaryEvent = string.Empty; }
    }

    [RelayCommand]
    private void AddDisplayField()
    {
        var v = NewDisplayField.Trim();
        if (!string.IsNullOrEmpty(v) && !DisplayFields.Contains(v)) { DisplayFields.Add(v); NewDisplayField = string.Empty; }
    }

    [RelayCommand]
    private void AddAnnounceField()
    {
        var v = NewAnnounceField.Trim();
        if (!string.IsNullOrEmpty(v) && !AnnounceFields.Contains(v)) { AnnounceFields.Add(v); NewAnnounceField = string.Empty; }
    }

    [RelayCommand] private void RemoveTriggeringEvent(string item) => TriggeringEvents.Remove(item);
    [RelayCommand] private void RemoveSecondaryEvent(string item)  => SecondaryEvents.Remove(item);
    [RelayCommand] private void RemoveDisplayField(string item)    => DisplayFields.Remove(item);
    [RelayCommand] private void RemoveAnnounceField(string item)   => AnnounceFields.Remove(item);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ManageCategories() => _navigation.ShowCategoryManagement();

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ValidationError = "Title is required.";
            return;
        }
        ValidationError = null;

        var model = BuildModel();
        if (_id == 0)
            await _repo.AddAsync(model);
        else
            await _repo.UpdateAsync(model);

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (_id > 0)
        {
            await _repo.DeleteAsync(_id);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private EventConfigurationModel BuildModel() => new()
    {
        Id = _id,
        Title = Title,
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
        CategoryId = SelectedCategory?.Id,
        IsEnabled = IsEnabled,
        TriggeringEvents = TriggeringEvents.ToList(),
        SecondaryEvents = SecondaryEvents.ToList(),
        SecondaryWaitTimeMs = SecondaryWaitTimeMs,
        Prompt = Prompt,
        ExpectedResultsSchema = string.IsNullOrWhiteSpace(ExpectedResultsSchema) ? null : ExpectedResultsSchema,
        TitleDisplayMode = Enum.Parse<TitleDisplayMode>(SelectedTitleDisplayMode),
        DisplayFields = DisplayFields.ToList(),
        DisplayKeys = DisplayKeys,
        AnnounceFields = AnnounceFields.ToList(),
        AnnounceKeys = AnnounceKeys,
        ShowTrayNotification = ShowTrayNotification,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    // Allows the navigation service to return a value indicating save vs cancel
    public interface INavigationCallback
    {
        void OnSaved();
        void OnCancelled();
    }
}
