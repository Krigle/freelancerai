namespace FreelanceFinderAI.Models;

public class ExtractedJobData
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string ExperienceLevel { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string SalaryRange { get; set; } = string.Empty;
    public string DescriptionSummary { get; set; } = string.Empty;
}

