using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface IJournalParser
{
    ParsedJournalEvent? TryParse(string rawLine);
}
