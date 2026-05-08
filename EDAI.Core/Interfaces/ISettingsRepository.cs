using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface ISettingsRepository
{
    Task<SettingsModel> GetAsync();
    Task SaveAsync(SettingsModel settings);
}
