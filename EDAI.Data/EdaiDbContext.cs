using System.Text.Json;
using EDAI.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EDAI.Data;

public sealed class EdaiDbContext : DbContext
{
    public EdaiDbContext(DbContextOptions<EdaiDbContext> options) : base(options) { }

    public DbSet<SettingEntity> Settings => Set<SettingEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<EventConfigurationEntity> EventConfigurations => Set<EventConfigurationEntity>();
    public DbSet<SessionHistoryEntity> SessionHistories => Set<SessionHistoryEntity>();
    public DbSet<ResponseLogEntity> ResponseLogs => Set<ResponseLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryEntity>(b =>
        {
            b.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<EventConfigurationEntity>(b =>
        {
            b.HasOne(e => e.Category)
             .WithMany(c => c.EventConfigurations)
             .HasForeignKey(e => e.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);

            ConfigureJsonList(b.Property(e => e.TriggeringEvents));
            ConfigureJsonList(b.Property(e => e.SecondaryEvents));
            ConfigureJsonList(b.Property(e => e.DisplayFields));
            ConfigureJsonList(b.Property(e => e.AnnounceFields));
        });

        modelBuilder.Entity<ResponseLogEntity>(b =>
        {
            b.HasOne(r => r.SessionHistory)
             .WithMany(s => s.ResponseLogs)
             .HasForeignKey(r => r.SessionHistoryId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(r => r.EventConfiguration)
             .WithMany()
             .HasForeignKey(r => r.EventConfigurationId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureJsonList(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<List<string>> property)
    {
        var converter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        var comparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        property.HasConversion(converter, comparer);
    }
}
