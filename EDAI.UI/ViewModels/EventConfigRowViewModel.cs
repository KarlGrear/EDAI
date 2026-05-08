using CommunityToolkit.Mvvm.ComponentModel;
using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed partial class EventConfigRowViewModel : ObservableObject
{
    public int Id { get; }
    public string Title { get; }
    public string? CategoryName { get; }
    public string TriggeringEventsSummary { get; }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private bool _isPendingSave;

    private readonly bool _originalIsEnabled;

    public EventConfigRowViewModel(EventConfigurationModel model)
    {
        Id = model.Id;
        Title = model.Title;
        CategoryName = model.CategoryName;
        TriggeringEventsSummary = string.Join(", ", model.TriggeringEvents);
        _isEnabled = model.IsEnabled;
        _originalIsEnabled = model.IsEnabled;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        IsPendingSave = value != _originalIsEnabled;
    }
}
