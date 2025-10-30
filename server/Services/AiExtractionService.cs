using System.Text;
using System.Text.Json;
using FreelanceFinderAI.Models;

namespace FreelanceFinderAI.Services;

public class AiExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly ILogger<AiExtractionService> _logger;

    public AiExtractionService(IConfiguration config, ILogger<AiExtractionService> logger)
    {
        _apiKey = config["OpenAI:ApiKey"] ?? "";
        _baseUrl = config["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        _model = config["OpenAI:Model"] ?? "gpt-4o-mini";
        _httpClient = new HttpClient();
        _logger = logger;
    }

    public async Task<ExtractedJobData> ExtractJobDataAsync(string text)
    {
        // If no API key is configured, return mock data
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured. Returning mock data.");
            return GetMockExtractedData(text);
        }

        try
        {
            var prompt = BuildExtractionPrompt(text);
            var payload = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a job data extraction assistant. Extract structured information from job postings and return ONLY valid JSON with no additional text." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Headers.Add("HTTP-Referer", "https://freelancefinderai.app");
            request.Headers.Add("X-Title", "FreelanceFinderAI");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            _logger.LogInformation($"Calling AI API at {_baseUrl} with model {_model}");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"AI API error: {result}");
                return GetMockExtractedData(text);
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
                    _logger.LogInformation($"Successfully parsed job data: {extractedData.Title} at {extractedData.Company}");
                    return extractedData;
                }
            }

            _logger.LogWarning("Failed to extract JSON from AI response, returning mock data");
            return GetMockExtractedData(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting job data");
            return GetMockExtractedData(text);
        }
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
  ""descriptionSummary"": ""brief 1-2 sentence summary""
}}

Job posting:
{jobText}

Return ONLY the JSON object, no additional text.";
    }

    private ExtractedJobData GetMockExtractedData(string text)
    {
        // Simple mock extraction based on text analysis
        return new ExtractedJobData
        {
            Title = "Job Position (AI extraction disabled)",
            Company = "Company Name",
            Skills = new List<string> { "Skill 1", "Skill 2", "Skill 3" },
            ExperienceLevel = "Mid-level",
            Location = "Remote",
            SalaryRange = "Not specified",
            DescriptionSummary = text.Length > 200 
                ? text.Substring(0, 200) + "..." 
                : text
        };
    }
}

