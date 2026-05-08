using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDAI.Data.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IDbContextFactory<EdaiDbContext> _factory;

    public CategoryRepository(IDbContextFactory<EdaiDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<CategoryModel>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
        return entities.Select(ToModel).ToList();
    }

    public async Task<CategoryModel> AddAsync(string name)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = new CategoryEntity { Name = name };
        context.Categories.Add(entity);
        await context.SaveChangesAsync();
        return ToModel(entity);
    }

    public async Task RenameAsync(int id, string newName)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Categories.FindAsync(id)
            ?? throw new InvalidOperationException($"Category {id} not found.");
        entity.Name = newName;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Categories.FindAsync(id);
        if (entity is not null)
        {
            context.Categories.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsInUseAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.EventConfigurations.AnyAsync(e => e.CategoryId == id);
    }

    private static CategoryModel ToModel(CategoryEntity e) => new(e.Id, e.Name);
}
