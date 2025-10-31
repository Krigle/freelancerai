using FreelanceFinderAI.Models;

namespace FreelanceFinderAI.Services;

public class JobSummaryGenerator : IJobSummaryGenerator
{
    public string GenerateStructuredSummary(string text, string title, string company, string experienceLevel, string location, string salaryRange)
    {
        // Create a comprehensive, well-structured summary
        var summaryParts = new List<string>();

        // Clean the text first and remove webpage noise
        var cleanedText = RemoveWebpageNoise(text);
        cleanedText = cleanedText
            .Replace("&nbsp;", " ")
            .Replace("&", "&")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        var lowerText = cleanedText.ToLower();

        // 1. Overview: Position and Company
        summaryParts.Add($"**{title}** at **{company}**");

        // 2. Key Details (Location, Experience, Salary)
        var details = new List<string>();
        if (location != "Not specified")
        {
            details.Add($"📍 {location}");
        }
        if (experienceLevel != "Not specified")
        {
            details.Add($"👤 {experienceLevel}");
        }
        if (salaryRange != "Not specified")
        {
            details.Add($"💰 {salaryRange}");
        }

        if (details.Count > 0)
        {
            summaryParts.Add(string.Join(" | ", details));
        }

        // 3. Extract About/Description section - look for first complete sentence about company
        // Try both cleaned and original text
        var aboutSection = ExtractCompanyDescription(cleanedText, company);
        if (string.IsNullOrEmpty(aboutSection))
        {
            aboutSection = ExtractCompanyDescription(text, company);
        }
        if (!string.IsNullOrEmpty(aboutSection))
        {
            summaryParts.Add($"\n**Overview:** {aboutSection}");
        }

        // 4. Extract Key Responsibilities - get bullet points
        var roleSection = ExtractBulletPoints(cleanedText, lowerText,
            new[] { "responsibilities:", "what you'll do", "you will:", "your role" },
            4);
        if (!string.IsNullOrEmpty(roleSection))
        {
            summaryParts.Add($"\n**Key Responsibilities:**\n{roleSection}");
        }

        // 5. Extract Requirements - get bullet points
        var requirementsSection = ExtractBulletPoints(cleanedText, lowerText,
            new[] { "minimum qualifications:", "qualifications:", "requirements:", "you have", "ideal candidate" },
            4);
        if (!string.IsNullOrEmpty(requirementsSection))
        {
            summaryParts.Add($"\n**Requirements:**\n{requirementsSection}");
        }

        // 6. Extract Benefits/Perks - get bullet points
        var benefitsSection = ExtractBulletPoints(cleanedText, lowerText,
            new[] { "benefits:", "what we offer:", "perks:", "we offer:" },
            3);
        if (!string.IsNullOrEmpty(benefitsSection))
        {
            summaryParts.Add($"\n**Benefits:**\n{benefitsSection}");
        }

        var summary = string.Join("\n", summaryParts);

        return summary;
    }

    private string RemoveWebpageNoise(string text)
    {
        // Find the "Full job description" marker and extract everything after it
        var fullJobDescIndex = text.IndexOf("Full job description", StringComparison.OrdinalIgnoreCase);

        string mainContent;
        if (fullJobDescIndex >= 0)
        {
            // Extract from "Full job description" onwards
            mainContent = text.Substring(fullJobDescIndex + "Full job description".Length).Trim();
        }
        else
        {
            // No marker found, use the whole text
            mainContent = text;
        }

        // Find where the footer starts and cut it off
        var footerMarkers = new[] { "Hiring Lab", "Career advice", "Browse jobs", "© 20", "ESG at Indeed" };
        int footerStart = -1;

        foreach (var marker in footerMarkers)
        {
            var index = mainContent.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && (footerStart == -1 || index < footerStart))
            {
                footerStart = index;
            }
        }

        if (footerStart >= 0)
        {
            mainContent = mainContent.Substring(0, footerStart).Trim();
        }

        return mainContent;
    }

    private string ExtractCompanyDescription(string text, string company)
    {
        // Create a flexible company name pattern (e.g., "Ameresco" matches "Ameresco, Inc.")
        var companyPattern = company.Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)[0];
        var lowerText = text.ToLower();
        var lowerPattern = companyPattern.ToLower();

        // Find where the company name appears with "is a" or "is the" - prioritize longer descriptions
        var patterns = new[] { " is a leading ", " is the leading ", " is a ", " is the ", " is an " };
        foreach (var pattern in patterns)
        {
            // Look for company name, then allow for optional text like ", Inc." or "(NYSE:...)", then the pattern
            var companyIndex = lowerText.IndexOf(lowerPattern);
            if (companyIndex < 0) continue;

            // Search for the pattern within 50 chars after the company name
            var searchStart = companyIndex;
            var searchEnd = Math.Min(companyIndex + 50, lowerText.Length);
            var searchSection = lowerText.Substring(searchStart, searchEnd - searchStart);
            var patternIndex = searchSection.IndexOf(pattern);

            if (patternIndex >= 0)
            {
                var index = companyIndex + patternIndex;
                // Find the start of the sentence (go back to previous period or start of text)
                // Start from the company name position, not the pattern position
                var sentenceStart = companyIndex;
                for (int i = companyIndex - 1; i >= 0; i--)
                {
                    if (text[i] == '.' || text[i] == '\n')
                    {
                        sentenceStart = i + 1;
                        break;
                    }
                    if (i == 0)
                    {
                        sentenceStart = 0;
                    }
                }

                // Find the end of the sentence (next period)
                var sentenceEnd = text.IndexOf('.', index + pattern.Length);
                if (sentenceEnd < 0) sentenceEnd = Math.Min(index + 300, text.Length);

                var sentence = text.Substring(sentenceStart, sentenceEnd - sentenceStart + 1).Trim();

                // Skip if it's just a legal statement
                if (sentence.ToLower().Contains("equal opportunity") ||
                    sentence.ToLower().Contains("employer") && sentence.Length < 100)
                {
                    continue;
                }

                // Truncate if too long
                if (sentence.Length > 300)
                {
                    sentence = sentence.Substring(0, 300).TrimEnd();
                    var lastSpace = sentence.LastIndexOf(' ');
                    if (lastSpace > 250)
                    {
                        sentence = sentence.Substring(0, lastSpace) + "...";
                    }
                }

                return sentence;
            }
        }

        return string.Empty;
    }

    private string ExtractBulletPoints(string text, string lowerText, string[] keywords, int maxBullets)
    {
        // Section headers that indicate the end of the current section
        var sectionHeaders = new[] {
            "responsibilities:", "qualifications:", "requirements:", "benefits:",
            "minimum qualifications:", "addition qualifications:", "what we offer:",
            "about the role:", "your role:", "what you'll do:", "perks:"
        };

        foreach (var keyword in keywords)
        {
            var startIndex = lowerText.IndexOf(keyword);
            if (startIndex >= 0)
            {
                // Find the actual start of the section (after the heading)
                var sectionStart = startIndex + keyword.Length;

                // Skip to next line
                var nextLineIndex = text.IndexOf('\n', sectionStart);
                if (nextLineIndex > 0)
                {
                    sectionStart = nextLineIndex + 1;
                }

                // Find the end of this section (next section header or 1000 chars)
                var endIndex = sectionStart + 1000;
                foreach (var header in sectionHeaders)
                {
                    if (header != keyword) // Don't stop at the same keyword
                    {
                        var headerIndex = lowerText.IndexOf(header, sectionStart);
                        if (headerIndex > sectionStart && headerIndex < endIndex)
                        {
                            endIndex = headerIndex;
                        }
                    }
                }
                endIndex = Math.Min(endIndex, text.Length);

                var section = text.Substring(sectionStart, endIndex - sectionStart);

                // Split into lines and find meaningful ones
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 10 && l.Length < 300) // Reasonable line length
                    .Where(l => !l.StartsWith("#")) // Exclude hashtags
                    .Where(l => !l.ToLower().Contains("official communications")) // Exclude disclaimers
                    .Where(l => !l.ToLower().Contains("equal opportunity")) // Exclude legal text
                    .Where(l => !l.ToLower().Contains("@") && !l.Contains(".com")) // Exclude email addresses
                    .Where(l => !l.ToLower().StartsWith("we are proud")) // Exclude benefits intro
                    .Where(l => !l.ToLower().StartsWith("we offer")) // Exclude benefits intro
                    .Take(maxBullets)
                    .ToList();

                if (lines.Any())
                {
                    // Format as bullet points
                    return string.Join("\n", lines.Select(l => $"• {l}"));
                }
            }
        }

        return string.Empty;
    }
}