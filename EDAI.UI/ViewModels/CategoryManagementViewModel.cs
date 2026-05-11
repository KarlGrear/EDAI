using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed partial class CategoryManagementViewModel : ObservableObject
{
    private readonly ICategoryRepository _repo;

    public ObservableCollection<CategoryModel> Categories { get; } = [];

    [ObservableProperty] private CategoryModel? _selectedCategory;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string? _errorMessage;

    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public CategoryManagementViewModel(ICategoryRepository repo)
    {
        _repo = repo;
    }

    public async Task LoadAsync()
    {
        Categories.Clear();
        var cats = await _repo.GetAllAsync();
        foreach (var c in cats) Categories.Add(c);
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        var name = EditName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        ErrorMessage = null;
        var created = await _repo.AddAsync(name);
        Categories.Add(created);
        EditName = string.Empty;
    }

    [RelayCommand]
    private async Task RenameCategory()
    {
        if (SelectedCategory == null) return;
        var name = EditName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        ErrorMessage = null;
        await _repo.RenameAsync(SelectedCategory.Id, name);

        var updated = SelectedCategory with { Name = name };
        var idx = Categories.IndexOf(SelectedCategory);
        if (idx >= 0) Categories[idx] = updated;
        SelectedCategory = updated;
    }

    [RelayCommand]
    private async Task DeleteCategory()
    {
        if (SelectedCategory == null) return;

        if (await _repo.IsInUseAsync(SelectedCategory.Id))
        {
            ErrorMessage = $"'{SelectedCategory.Name}' is used by existing event configurations.";
            return;
        }

        ErrorMessage = null;
        await _repo.DeleteAsync(SelectedCategory.Id);
        Categories.Remove(SelectedCategory);
        SelectedCategory = null;
    }

    partial void OnSelectedCategoryChanged(CategoryModel? value)
    {
        if (value != null) EditName = value.Name;
    }
}
