using FreelanceFinderAI.Models;

namespace FreelanceFinderAI.Services;

public interface IJobSummaryGenerator
{
    string GenerateStructuredSummary(string text, string title, string company, string experienceLevel, string location, string salaryRange);
}