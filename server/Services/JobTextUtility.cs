using System;

namespace FreelanceFinderAI.Services;

public static class JobTextUtility
{
    public static string RemoveWebpageNoise(string text)
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

    public static string ExtractBulletPoints(string text, string lowerText, string[] keywords, int maxBullets)
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