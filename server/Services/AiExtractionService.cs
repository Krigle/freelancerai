using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using FreelanceFinderAI.Models;

namespace FreelanceFinderAI.Services;

public class AiExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly AiExtractionOptions _options;
    private readonly ILogger<AiExtractionService> _logger;
    private readonly IJobTextPreprocessor _textPreprocessor;
    private readonly IMemoryCache _cache;

    public AiExtractionService(
        AiExtractionOptions options,
        ILogger<AiExtractionService> logger,
        IJobTextPreprocessor textPreprocessor,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textPreprocessor = textPreprocessor ?? throw new ArgumentNullException(nameof(textPreprocessor));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _httpClient = httpClientFactory.CreateClient("AiApiClient");
    }

    public async Task<ExtractedJobData> ExtractJobDataAsync(string text)
    {
        // Input validation
        if (!_textPreprocessor.IsValidInput(text))
        {
            _logger.LogWarning("Invalid input provided for job data extraction");
            throw new ArgumentException("Invalid input text", nameof(text));
        }

        // Check cache first
        var cacheKey = $"JobData_{text.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out ExtractedJobData cachedResult))
        {
            _logger.LogInformation("Returning cached job data");
            return cachedResult;
        }

        // Preprocess text
        var processedText = _textPreprocessor.RemoveHtmlEntities(text);
        processedText = _textPreprocessor.NormalizeWhitespace(processedText);
        processedText = _textPreprocessor.RemoveWebpageNoise(processedText);

        // Validate API key is configured
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogError("OpenAI API key not configured. Cannot extract job data.");
            throw new InvalidOperationException("OpenAI API key is not configured. Please configure the API key in appsettings.json");
        }

        // AI extraction with resilience policies handled by HttpClient
        var result = await CallAiApiAsync(processedText);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
        return result;
    }

    private async Task<ExtractedJobData> CallAiApiAsync(string text)
    {
        var prompt = BuildExtractionPrompt(text);
        var payload = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = "You are a job data extraction assistant. Extract structured information from job postings and return ONLY valid JSON with no additional text." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
        request.Headers.Add("HTTP-Referer", "https://freelancefinderai.app");
        request.Headers.Add("X-Title", "FreelanceFinderAI");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        _logger.LogInformation($"Calling AI API at {_options.BaseUrl} with model {_options.Model}");
        _logger.LogInformation($"API Key configured: {!string.IsNullOrEmpty(_options.ApiKey)} (length: {_options.ApiKey?.Length ?? 0})");

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"AI API error: {response.StatusCode} - {result}");
            throw new HttpRequestException($"AI API error: {response.StatusCode} - {result}");
        }

        _logger.LogInformation($"AI API response: {result}");

        // Parse the OpenAI/OpenRouter response
        var openAiResponse = JsonSerializer.Deserialize<JsonElement>(result);
        var content = openAiResponse
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        _logger.LogInformation($"AI extracted content: {content}");

        // Extract JSON from the content
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            _logger.LogInformation($"Extracted JSON: {json}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var extractedData = JsonSerializer.Deserialize<ExtractedJobData>(json, options);

            if (extractedData != null)
            {
                // Validate extracted data
                if (string.IsNullOrWhiteSpace(extractedData.Title))
                {
                    extractedData.Title = "Job Position";
                }
                if (string.IsNullOrWhiteSpace(extractedData.Company))
                {
                    extractedData.Company = "Company Name";
                }
                if (extractedData.Skills == null || extractedData.Skills.Count == 0)
                {
                    extractedData.Skills = new List<string> { "See job description" };
                }

                _logger.LogInformation($"Successfully parsed job data: {extractedData.Title} at {extractedData.Company}");
                return extractedData;
            }
        }

        throw new InvalidOperationException("Failed to extract valid JSON from AI response");
    }

    private string BuildExtractionPrompt(string jobText)
    {
        return $@"Extract structured job data from the following job posting and return ONLY a JSON object with these exact fields:

{{
  ""title"": ""job title"",
  ""company"": ""company name"",
  ""skills"": [""skill1"", ""skill2""],
  ""experienceLevel"": ""entry/mid/senior/lead"",
  ""location"": ""location or remote"",
  ""salaryRange"": ""salary range or empty string"",
  ""descriptionSummary"": ""Create a well-formatted summary using this EXACT structure:

**About:** [1-2 sentence company description if available]

**The Role:**
• [Key responsibility 1]
• [Key responsibility 2]
• [Key responsibility 3]

**Requirements:**
• [Requirement 1]
• [Requirement 2]
• [Requirement 3]

**Benefits:** [Benefits if mentioned, otherwise omit this section]

Use bullet points (•) for lists. Keep each bullet point concise (under 100 characters). Include only the most important 3-4 items per section. If a section is not mentioned in the job posting, omit it entirely.""
}}

Job posting:
{jobText}

Return ONLY the JSON object, no additional text.";
    }

    private ExtractedJobData GetMockExtractedData(string text)
    {
        // Intelligent mock extraction based on text analysis
        // Clean up the text first - remove HTML entities, extra whitespace, etc.
        var cleanedText = text
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        // For title/company extraction, use the original cleaned text (before noise removal)
        var originalLines = cleanedText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        // For summary, remove webpage noise
        var cleanedForSummary = JobTextUtility.RemoveWebpageNoise(cleanedText);

        var lines = originalLines; // Use original for title/company extraction
        var lowerText = cleanedText.ToLower();

        // Extract title - look for job title patterns
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

        // Extract company - look for company name patterns
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

        // Extract skills (common tech keywords)
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

        // Extract experience level
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

        // Extract location
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

        // Extract salary - support both $ and £
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

        // Create summary - extract key information intelligently (use cleaned text for summary)
        var summary = CreateIntelligentSummary(cleanedForSummary, title, company, experienceLevel, location, salaryRange);

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


    private string CreateIntelligentSummary(string text, string title, string company, string experienceLevel, string location, string salaryRange)
    {
        // Create a comprehensive, well-structured summary
        var summaryParts = new List<string>();

        // Clean the text first and remove webpage noise
        var cleanedText = JobTextUtility.RemoveWebpageNoise(text);
        cleanedText = cleanedText
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        var lowerText = cleanedText.ToLower();

        // 1. Header: Position and Company
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
            summaryParts.Add($"\n**About:** {aboutSection}");
        }

        // 4. Extract Role/Responsibilities - get bullet points
        var roleSection = JobTextUtility.ExtractBulletPoints(cleanedText, lowerText,
            new[] { "responsibilities:", "what you'll do", "you will:", "your role" },
            4);
        if (!string.IsNullOrEmpty(roleSection))
        {
            summaryParts.Add($"\n**The Role:**\n{roleSection}");
        }

        // 5. Extract Requirements - get bullet points
        var requirementsSection = JobTextUtility.ExtractBulletPoints(cleanedText, lowerText,
            new[] { "minimum qualifications:", "qualifications:", "requirements:", "you have", "ideal candidate" },
            4);
        if (!string.IsNullOrEmpty(requirementsSection))
        {
            summaryParts.Add($"\n**Requirements:**\n{requirementsSection}");
        }

        // 6. Extract Benefits/Perks - get bullet points
        var benefitsSection = JobTextUtility.ExtractBulletPoints(cleanedText, lowerText,
            new[] { "benefits:", "what we offer:", "perks:", "we offer:" },
            3);
        if (!string.IsNullOrEmpty(benefitsSection))
        {
            summaryParts.Add($"\n**Benefits:**\n{benefitsSection}");
        }

        var summary = string.Join("\n", summaryParts);

        return summary;
    }

    private string ExtractSection(string text, string lowerText, string[] keywords, int maxLength)
    {
        foreach (var keyword in keywords)
        {
            var startIndex = lowerText.IndexOf(keyword);
            if (startIndex >= 0)
            {
                // Find the actual start of the section (after the heading)
                var sectionStart = startIndex + keyword.Length;

                // Skip to next line if the keyword is a heading
                var nextLineIndex = text.IndexOf('\n', sectionStart);
                if (nextLineIndex > 0 && nextLineIndex - sectionStart < 50)
                {
                    sectionStart = nextLineIndex + 1;
                }

                // Extract text up to maxLength or next section heading
                var endIndex = Math.Min(sectionStart + maxLength * 3, text.Length);
                var section = text.Substring(sectionStart, endIndex - sectionStart);

                // Clean up the section - be more lenient with line filtering
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 5) // More lenient - accept shorter lines
                    .Where(l => !l.StartsWith("#")) // Exclude hashtags
                    .Take(5) // Take more lines
                    .ToList();

                if (lines.Any())
                {
                    var result = string.Join(" ", lines);

                    // Truncate to maxLength
                    if (result.Length > maxLength)
                    {
                        result = result.Substring(0, maxLength).TrimEnd();
                        // Try to end at a word boundary
                        var lastSpace = result.LastIndexOf(' ');
                        if (lastSpace > maxLength - 50)
                        {
                            result = result.Substring(0, lastSpace);
                        }
                        result += "...";
                    }

                    return result;
                }
            }
        }

        return string.Empty;
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

}

