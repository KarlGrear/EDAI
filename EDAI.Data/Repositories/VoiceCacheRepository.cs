using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDAI.Data.Repositories;

public sealed class VoiceCacheRepository : IVoiceCacheRepository
{
    private readonly IDbContextFactory<EdaiDbContext> _factory;

    public VoiceCacheRepository(IDbContextFactory<EdaiDbContext> factory) => _factory = factory;

    public async Task<VoiceCacheModel?> GetByHashAsync(string hash)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var e = await ctx.VoiceCache.FindAsync(hash);
        return e is null ? null : ToModel(e);
    }

    public async Task InsertAsync(VoiceCacheModel entry)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.VoiceCache.Add(ToEntity(entry));
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateUsageAsync(string hash)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var e = await ctx.VoiceCache.FindAsync(hash);
        if (e is null) return;
        e.LastUsed = DateTime.UtcNow;
        e.UseCount++;
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteByHashAsync(string hash)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var e = await ctx.VoiceCache.FindAsync(hash);
        if (e is null) return;
        ctx.VoiceCache.Remove(e);
        await ctx.SaveChangesAsync();
    }

    public async Task<int> ClearAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.VoiceCache.ExecuteDeleteAsync();
    }

    private static VoiceCacheModel ToModel(VoiceCacheEntity e) => new()
    {
        Hash      = e.Hash,
        Phrase    = e.Phrase,
        VoiceName = e.VoiceName,
        Language  = e.Language,
        Rate      = e.Rate,
        Pitch     = e.Pitch,
        FilePath  = e.FilePath,
        CreatedAt = e.CreatedAt,
        LastUsed  = e.LastUsed,
        UseCount  = e.UseCount,
    };

    private static VoiceCacheEntity ToEntity(VoiceCacheModel m) => new()
    {
        Hash      = m.Hash,
        Phrase    = m.Phrase,
        VoiceName = m.VoiceName,
        Language  = m.Language,
        Rate      = m.Rate,
        Pitch     = m.Pitch,
        FilePath  = m.FilePath,
        CreatedAt = m.CreatedAt,
        LastUsed  = m.LastUsed,
        UseCount  = m.UseCount,
    };
}
