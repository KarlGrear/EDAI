using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface IEventConfigurationRepository
{
    Task<IReadOnlyList<EventConfigurationModel>> GetAllAsync();
    Task<IReadOnlyList<EventConfigurationModel>> GetEnabledAsync();
    Task<EventConfigurationModel?> GetByIdAsync(int id);
    Task<EventConfigurationModel> AddAsync(EventConfigurationModel model);
    Task UpdateAsync(EventConfigurationModel model);
    Task DeleteAsync(int id);
    Task SetEnabledAsync(int id, bool isEnabled);
}
