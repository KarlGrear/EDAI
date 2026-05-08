using EDAI.Core.Models;

namespace EDAI.Data.Entities;

public sealed class EventConfigurationEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public CategoryEntity? Category { get; set; }
    public bool IsEnabled { get; set; }
    public List<string> TriggeringEvents { get; set; } = [];
    public List<string> SecondaryEvents { get; set; } = [];
    public int SecondaryWaitTimeMs { get; set; } = 1000;
    public string Prompt { get; set; } = string.Empty;
    public string? ExpectedResultsSchema { get; set; }
    public TitleDisplayMode TitleDisplayMode { get; set; }
    public List<string> DisplayFields { get; set; } = [];
    public bool DisplayKeys { get; set; }
    public List<string> AnnounceFields { get; set; } = [];
    public bool AnnounceKeys { get; set; }
    public bool ShowTrayNotification { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
