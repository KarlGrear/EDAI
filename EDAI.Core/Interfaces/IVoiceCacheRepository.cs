using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface IVoiceCacheRepository
{
    Task<VoiceCacheModel?> GetByHashAsync(string hash);
    Task InsertAsync(VoiceCacheModel entry);
    Task UpdateUsageAsync(string hash);
    Task DeleteByHashAsync(string hash);
    Task<int> ClearAllAsync();
}
