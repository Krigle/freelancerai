using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FreelanceFinderAI.Models;

public class Job
{
    public int Id { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    
    [Column(TypeName = "TEXT")]
    public string ExtractedJson { get; set; } = string.Empty;
    
    [NotMapped]
    public ExtractedJobData? Extracted
    {
        get => string.IsNullOrEmpty(ExtractedJson) 
            ? null 
            : JsonSerializer.Deserialize<ExtractedJobData>(ExtractedJson);
        set => ExtractedJson = value == null 
            ? string.Empty 
            : JsonSerializer.Serialize(value);
    }
    
    public DateTime CreatedAt { get; set; }
}

