using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryModel>> GetAllAsync();
    Task<CategoryModel> AddAsync(string name);
    Task RenameAsync(int id, string newName);
    Task DeleteAsync(int id);
    Task<bool> IsInUseAsync(int id);
}
