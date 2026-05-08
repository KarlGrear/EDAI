using System.Collections.ObjectModel;
using System.ComponentModel;
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

    public ObservableCollection<EventConfigRowViewModel> Rows { get; } = [];
    public ICollectionView RowsView { get; }

    public ObservableCollection<CategoryModel?> Categories { get; } = [];

    [ObservableProperty] private CategoryModel? _selectedCategory;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private EventConfigRowViewModel? _selectedRow;

    public EventConfigSelectionViewModel(
        IEventConfigurationRepository repo,
        ICategoryRepository categoryRepo,
        INavigationService navigation)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _navigation = navigation;

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = ApplyFilter;
    }

    public async Task LoadAsync()
    {
        Rows.Clear();
        Categories.Clear();
        Categories.Add(null);

        var cats = await _categoryRepo.GetAllAsync();
        foreach (var c in cats) Categories.Add(c);

        var configs = await _repo.GetAllAsync();
        foreach (var m in configs) Rows.Add(new EventConfigRowViewModel(m));
    }

    partial void OnSelectedCategoryChanged(CategoryModel? value) => RowsView.Refresh();
    partial void OnSearchTextChanged(string value) => RowsView.Refresh();

    private bool ApplyFilter(object obj)
    {
        if (obj is not EventConfigRowViewModel row) return false;

        if (SelectedCategory != null && row.CategoryName != SelectedCategory.Name)
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
    private void ManageCategories() => _navigation.ShowCategoryManagement();
}
