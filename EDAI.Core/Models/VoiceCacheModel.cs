namespace EDAI.Core.Models;

public sealed class VoiceCacheModel
{
    public string Hash { get; set; } = string.Empty;
    public string Phrase { get; set; } = string.Empty;
    public string VoiceName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public double Rate { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsed { get; set; }
    public int UseCount { get; set; }
}
