using FreelanceFinderAI.Models;

namespace FreelanceFinderAI.Services;

public interface IJobDataExtractor
{
    ExtractedJobData ExtractJobData(string text);
}