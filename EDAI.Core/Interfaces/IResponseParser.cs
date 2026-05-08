using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface IResponseParser
{
    AiResponse Parse(string rawJson, EventConfigurationModel config);
}
