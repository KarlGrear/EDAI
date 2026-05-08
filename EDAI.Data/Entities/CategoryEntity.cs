namespace EDAI.Data.Entities;

public sealed class CategoryEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<EventConfigurationEntity> EventConfigurations { get; set; } = [];
}
