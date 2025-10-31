using System;

namespace FreelanceFinderAI.Services
{
    public interface IJobTextPreprocessor
    {
        string RemoveHtmlEntities(string input);
        string NormalizeWhitespace(string input);
        string RemoveWebpageNoise(string input);
        bool IsValidInput(string input);
    }
}