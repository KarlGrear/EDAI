using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface IResponseLogRepository
{
    Task<ResponseLogModel> AddAsync(ResponseLogModel model);
    Task<IReadOnlyList<ResponseLogModel>> GetRecentAsync(int count);
}
