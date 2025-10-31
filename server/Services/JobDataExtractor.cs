using FreelanceFinderAI.Models;

namespace FreelanceFinderAI.Services;

public class JobDataExtractor : IJobDataExtractor
{
    private readonly IJobSummaryGenerator _summaryGenerator;

    public JobDataExtractor(IJobSummaryGenerator summaryGenerator)
    {
        _summaryGenerator = summaryGenerator;
    }
    public ExtractedJobData ExtractJobData(string text)
    {
        // Clean up the text first - remove HTML entities, extra whitespace, etc.
        var cleanedText = text
            .Replace("&nbsp;", " ")
            .Replace("&", "&")
            .Replace("<", "<")
            .Replace(">", ">")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        // For title/company extraction, use the original cleaned text (before noise removal)
        var originalLines = cleanedText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        // For summary, remove webpage noise
        var cleanedForSummary = cleanedText;

        var lines = originalLines; // Use original for title/company extraction
        var lowerText = cleanedText.ToLower();

        // Extract title - look for job title patterns
        var title = ExtractTitle(lines);

        // Extract company - look for company name patterns
        var company = ExtractCompany(cleanedText, lines, title);

        // Extract skills (common tech keywords)
        var skills = ExtractSkills(lowerText);

        // Extract experience level
        var experienceLevel = ExtractExperienceLevel(lowerText);

        // Extract location
        var location = ExtractLocation(lowerText);

        // Extract salary - support both $ and £
        var salaryRange = ExtractSalaryRange(cleanedText);

        // Create summary - extract key information intelligently (use cleaned text for summary)
        var summary = _summaryGenerator.GenerateStructuredSummary(cleanedForSummary, title, company, experienceLevel, location, salaryRange);

        return new ExtractedJobData
        {
            Title = title,
            Company = company,
            Skills = skills,
            ExperienceLevel = experienceLevel,
            Location = location,
            SalaryRange = salaryRange,
            DescriptionSummary = summary
        };
    }

    private string ExtractTitle(string[] lines)
    {
        var title = "Job Position";

        // Try to find a line that looks like a job title (contains developer, engineer, manager, etc.)
        var jobTitleKeywords = new[] { "developer", "engineer", "designer", "manager", "analyst", "architect", "lead", "senior", "junior", "specialist", "consultant" };
        foreach (var line in lines)
        {
            var lineLower = line.ToLower();
            if (jobTitleKeywords.Any(keyword => lineLower.Contains(keyword)) &&
                line.Length < 100 &&
                line.Length > 5 &&
                !lineLower.Contains("what") &&
                !lineLower.Contains("where") &&
                !lineLower.Contains("job title") &&
                !lineLower.Contains("keywords") &&
                !line.Contains("**") && // Exclude markdown
                !line.Contains(" at ") && // Exclude "Title at Company" format
                !line.Contains("📍") && // Exclude emoji lines
                !line.Contains("👤"))
            {
                title = line;
                break;
            }
        }

        return title;
    }

    private string ExtractCompany(string cleanedText, string[] lines, string title)
    {
        var company = "Company Name";

        // First: Try company suffixes (Inc., Corp, LLC, Ltd., etc.) - most reliable
        var companyMatch = System.Text.RegularExpressions.Regex.Match(cleanedText, @"\b([A-Z][A-Za-z0-9&\s]+\s+(?:Inc\.?|Corp\.?|Corporation|LLC|Ltd\.?|Limited|Co\.))\b");
        if (companyMatch.Success)
        {
            company = companyMatch.Groups[1].Value.Trim();
        }

        // Second: Try "at Company" pattern
        if (company == "Company Name")
        {
            var atMatch = System.Text.RegularExpressions.Regex.Match(cleanedText, @"(?:at|@)\s+([A-Z][A-Za-z0-9\s&.,]+?)(?:\n|$|,|\s-)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (atMatch.Success && atMatch.Groups[1].Value.Trim().Length > 0 && atMatch.Groups[1].Value.Trim().Length < 50)
            {
                var potentialCompany = atMatch.Groups[1].Value.Trim();
                // Filter out common noise phrases
                var noisePhrases = new[] { "job title", "keywords", "company", "indeed", "glassdoor" };
                if (!noisePhrases.Any(phrase => potentialCompany.ToLower().Contains(phrase)))
                {
                    company = potentialCompany;
                }
            }
        }

        // Third: Look for a line that appears right after the job title
        if (company == "Company Name")
        {
            var titleIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    titleIndex = i;
                    break;
                }
            }

            if (titleIndex >= 0 && titleIndex + 1 < lines.Length)
            {
                var potentialCompany = lines[titleIndex + 1];
                // Check if it looks like a company name
                if (potentialCompany.Length > 2 &&
                    potentialCompany.Length < 100 &&
                    char.IsUpper(potentialCompany[0]) &&
                    !potentialCompany.ToLower().Contains("remote") &&
                    !potentialCompany.ToLower().Contains("hybrid") &&
                    !potentialCompany.ToLower().Contains("location") &&
                    !potentialCompany.ToLower().Contains("salary") &&
                    !potentialCompany.ToLower().Contains("user research") &&
                    !potentialCompany.ToLower().Contains("responsive") &&
                    !potentialCompany.ToLower().Contains("skills") &&
                    !potentialCompany.ToLower().Contains("£") &&
                    !potentialCompany.ToLower().Contains("$") &&
                    !potentialCompany.Contains(".") && // Likely a rating like "3.3"
                    !System.Text.RegularExpressions.Regex.IsMatch(potentialCompany, @"^\d"))
                {
                    company = potentialCompany;
                }
            }
        }

        return company;
    }

    private List<string> ExtractSkills(string lowerText)
    {
        var skills = new List<string>();
        var techKeywords = new[] {
            "React", "Angular", "Vue", "JavaScript", "TypeScript", "Node.js", "Python", "Java", "C#", ".NET",
            "AWS", "Azure", "GCP", "Docker", "Kubernetes", "SQL", "MongoDB", "PostgreSQL", "Redis",
            "Git", "CI/CD", "Agile", "Scrum", "REST", "GraphQL", "HTML", "CSS", "Tailwind", "Bootstrap"
        };

        foreach (var keyword in techKeywords)
        {
            if (lowerText.Contains(keyword.ToLower()))
            {
                skills.Add(keyword);
            }
        }

        if (skills.Count == 0)
        {
            skills.Add("See job description");
        }

        return skills;
    }

    private string ExtractExperienceLevel(string lowerText)
    {
        var experienceLevel = "Not specified";
        if (lowerText.Contains("senior") || lowerText.Contains("sr.") || lowerText.Contains("lead"))
        {
            experienceLevel = "Senior";
        }
        else if (lowerText.Contains("junior") || lowerText.Contains("jr.") || lowerText.Contains("entry"))
        {
            experienceLevel = "Entry-level";
        }
        else if (lowerText.Contains("mid") || lowerText.Contains("intermediate"))
        {
            experienceLevel = "Mid-level";
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(lowerText, @"\d+\+?\s*years?"))
        {
            var yearsMatch = System.Text.RegularExpressions.Regex.Match(lowerText, @"(\d+)\+?\s*years?");
            if (yearsMatch.Success)
            {
                var years = int.Parse(yearsMatch.Groups[1].Value);
                experienceLevel = years >= 5 ? "Senior" : years >= 2 ? "Mid-level" : "Entry-level";
            }
        }

        return experienceLevel;
    }

    private string ExtractLocation(string lowerText)
    {
        var location = "Not specified";
        if (lowerText.Contains("remote"))
        {
            location = "Remote";
        }
        else if (lowerText.Contains("hybrid"))
        {
            location = "Hybrid";
        }
        else if (lowerText.Contains("on-site") || lowerText.Contains("onsite") || lowerText.Contains("office"))
        {
            location = "On-site";
        }

        return location;
    }

    private string ExtractSalaryRange(string cleanedText)
    {
        var salaryRange = "Not specified";

        // Try range with $ or £
        var salaryMatch = System.Text.RegularExpressions.Regex.Match(cleanedText, @"[£$]\s*(\d{1,3}(?:,\d{3})*(?:k|K)?)\s*(?:-|to)\s*[£$]?\s*(\d{1,3}(?:,\d{3})*(?:k|K)?)");
        if (salaryMatch.Success)
        {
            var currency = cleanedText.Contains("£") ? "£" : "$";
            salaryRange = $"{currency}{salaryMatch.Groups[1].Value} - {currency}{salaryMatch.Groups[2].Value}";
        }
        else
        {
            // Try single value with $ or £
            salaryMatch = System.Text.RegularExpressions.Regex.Match(cleanedText, @"[£$]\s*(\d{1,3}(?:,\d{3})*(?:k|K)?)");
            if (salaryMatch.Success)
            {
                var currency = cleanedText.Contains("£") ? "£" : "$";
                salaryRange = $"{currency}{salaryMatch.Groups[1].Value}";
            }
        }

        return salaryRange;
    }

}