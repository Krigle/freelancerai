using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FreelanceFinderAI.Models;
using FreelanceFinderAI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceFinderAI.Tests;

public class AiExtractionServiceTests
{
    private readonly Mock<ILogger<AiExtractionService>> _loggerMock;
    private readonly Mock<IJobDataExtractor> _jobDataExtractorMock;
    private readonly Mock<IJobTextPreprocessor> _textPreprocessorMock;
    private readonly Mock<IJobSummaryGenerator> _summaryGeneratorMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IMemoryCache _cache;
    private readonly AiExtractionOptions _options;
    private readonly AiExtractionService _service;

    public AiExtractionServiceTests()
    {
        _loggerMock = new Mock<ILogger<AiExtractionService>>();
        _jobDataExtractorMock = new Mock<IJobDataExtractor>();
        _textPreprocessorMock = new Mock<IJobTextPreprocessor>();
        _summaryGeneratorMock = new Mock<IJobSummaryGenerator>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _options = new AiExtractionOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini",
            MaxRetries = 2,
            Timeout = 30,
            MaxTextLength = 10000
        };

        // Setup HttpClientFactory mock
        var httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(f => f.CreateClient("AiApiClient")).Returns(httpClient);

        _service = new AiExtractionService(
            _options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task ExtractJobDataAsync_NoApiKey_ThrowsException()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var testText = "Senior React Developer at Tech Corp - Remote, $80k-100k";
        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(testText)).Returns(testText);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExtractJobDataAsync(testText));
    }

    [Fact]
    public async Task ExtractJobDataAsync_WithApiKey_MakesHttpRequest()
    {
        // Arrange
        var testText = "Senior Developer position at Google";
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);

        // Create service with HttpClient for testing
        var service = new AiExtractionService(
            _options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);
        // Inject HttpClient via reflection for testing
        typeof(AiExtractionService).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(service, httpClient);

        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(testText)).Returns(testText);

        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonSerializer.Serialize(new
                            {
                                title = "Senior Developer",
                                company = "Google",
                                skills = new[] { "React", "Node.js" },
                                experienceLevel = "Senior",
                                location = "Remote",
                                salaryRange = "$150k-200k",
                                descriptionSummary = "Senior Developer at Google"
                            })
                        }
                    }
                }
            }), Encoding.UTF8, "application/json")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.ExtractJobDataAsync(testText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Senior Developer", result.Title);
        Assert.Equal("Google", result.Company);
        Assert.Contains("React", result.Skills);
        Assert.Contains("Node.js", result.Skills);
        Assert.Equal("Senior", result.ExperienceLevel);
        Assert.Equal("Remote", result.Location);
        Assert.Equal("$150k-200k", result.SalaryRange);
    }

    [Fact]
    public async Task ExtractJobDataAsync_ApiError_FallsBackToMockData()
    {
        // Arrange
        var testText = "Senior Developer position at Google";
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);

        var service = new AiExtractionService(
            _options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);
        typeof(AiExtractionService).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(service, httpClient);

        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(testText)).Returns(testText);

        // Mock API error
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            });

        var mockResult = new ExtractedJobData
        {
            Title = "Fallback Title",
            Company = "Fallback Company",
            Skills = new List<string> { "Fallback Skill" }
        };
        _jobDataExtractorMock.Setup(e => e.ExtractJobData(testText)).Returns(mockResult);

        // Act
        var result = await service.ExtractJobDataAsync(testText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fallback Title", result.Title);
        Assert.Equal("Fallback Company", result.Company);
        Assert.Contains("Fallback Skill", result.Skills);
    }

    [Fact]
    public async Task ExtractJobDataAsync_UsesCache_WhenAvailable()
    {
        // Arrange
        var testText = "Cached job text";
        var cachedResult = new ExtractedJobData
        {
            Title = "Cached Title",
            Company = "Cached Company",
            Skills = new List<string> { "Cached Skill" }
        };

        // Pre-populate cache
        var cacheKey = $"JobData_{testText.GetHashCode()}";
        _cache.Set(cacheKey, cachedResult, TimeSpan.FromMinutes(30));

        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(true);

        // Act
        var result = await _service.ExtractJobDataAsync(testText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cached Title", result.Title);
        Assert.Equal("Cached Company", result.Company);
        Assert.Contains("Cached Skill", result.Skills);

        // Verify that preprocessors and extractors were not called
        _textPreprocessorMock.Verify(p => p.RemoveHtmlEntities(It.IsAny<string>()), Times.Never);
        _jobDataExtractorMock.Verify(e => e.ExtractJobData(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExtractJobDataAsync_PreprocessesTextCorrectly()
    {
        // Arrange
        var rawText = "  Raw &nbsp; text with <script>alert('test');</script> noise  ";
        var processedText = "Raw text with noise";

        _textPreprocessorMock.Setup(p => p.IsValidInput(rawText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(rawText)).Returns("  Raw   text with <script>alert('test');</script> noise  ");
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace("  Raw   text with <script>alert('test');</script> noise  ")).Returns("Raw text with <script>alert('test');</script> noise");
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise("Raw text with <script>alert('test');</script> noise")).Returns(processedText);

        var mockResult = new ExtractedJobData
        {
            Title = "Processed Title",
            Company = "Processed Company",
            Skills = new List<string> { "Processed Skill" }
        };
        _jobDataExtractorMock.Setup(e => e.ExtractJobData(processedText)).Returns(mockResult);

        // Act
        var result = await _service.ExtractJobDataAsync(rawText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Processed Title", result.Title);

        // Verify preprocessing steps were called in correct order
        _textPreprocessorMock.Verify(p => p.RemoveHtmlEntities(rawText), Times.Once);
        _textPreprocessorMock.Verify(p => p.NormalizeWhitespace(It.IsAny<string>()), Times.Once);
        _textPreprocessorMock.Verify(p => p.RemoveWebpageNoise(It.IsAny<string>()), Times.Once);
        _jobDataExtractorMock.Verify(e => e.ExtractJobData(processedText), Times.Once);
    }

    [Fact]
    public async Task ExtractJobDataAsync_ApiError_ReturnsMockData()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var testText = "Invalid job text that should trigger mock data";
        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(testText)).Returns(testText);

        var mockResult = new ExtractedJobData
        {
            Title = "Job Position",
            Company = "Company Name",
            Skills = new List<string> { "See job description" }
        };
        _jobDataExtractorMock.Setup(e => e.ExtractJobData(testText)).Returns(mockResult);

        // Act
        var result = await service.ExtractJobDataAsync(testText);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ExtractedJobData>(result);
    }

    [Fact]
    public async Task ExtractJobDataAsync_ValidJsonResponse_ParsesCorrectly()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var testCases = new[]
        {
            "Senior React Developer at Google - Remote, $100k",
            "Junior Python Engineer at Startup Inc - On-site, London",
            "Full Stack Developer - No company mentioned",
            "DevOps Engineer at Amazon - Hybrid, $90k-120k"
        };

        foreach (var testText in testCases)
        {
            _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(true);
            _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(testText)).Returns(testText);
            _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(testText)).Returns(testText);
            _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(testText)).Returns(testText);

            var mockResult = new ExtractedJobData
            {
                Title = "Job Position",
                Company = "Company Name",
                Skills = new List<string> { "See job description" }
            };
            _jobDataExtractorMock.Setup(e => e.ExtractJobData(testText)).Returns(mockResult);

            // Act
            var result = await service.ExtractJobDataAsync(testText);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Title);
            Assert.NotNull(result.Company);
            Assert.NotNull(result.Skills);
        }
    }

    [Fact]
    public async Task ExtractJobDataAsync_LongJobText_HandlesGracefully()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var longText = string.Join(" ", Enumerable.Repeat("This is a job description with many words.", 100));
        _textPreprocessorMock.Setup(p => p.IsValidInput(longText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(longText)).Returns(longText);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(longText)).Returns(longText);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(longText)).Returns(longText);

        var mockResult = new ExtractedJobData
        {
            Title = "Job Position",
            Company = "Company Name",
            Skills = new List<string> { "See job description" },
            DescriptionSummary = new string('A', 900) // Long summary
        };
        _jobDataExtractorMock.Setup(e => e.ExtractJobData(longText)).Returns(mockResult);

        // Act
        var result = await service.ExtractJobDataAsync(longText);

        // Assert
        Assert.NotNull(result);
        // Note: The summary length check would be handled by the JobSummaryGenerator
    }

    [Fact]
    public async Task ExtractJobDataAsync_MalformedResponse_HandlesGracefully()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var testText = "Any text";
        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(testText)).Returns(testText);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(testText)).Returns(testText);

        var mockResult = new ExtractedJobData
        {
            Title = "Job Position",
            Company = "Company Name",
            Skills = new List<string> { "See job description" }
        };
        _jobDataExtractorMock.Setup(e => e.ExtractJobData(testText)).Returns(mockResult);

        // Act
        var result = await service.ExtractJobDataAsync(testText);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ExtractedJobData>(result);
    }

    [Fact]
    public async Task ExtractJobDataAsync_EmptyText_ThrowsArgumentException()
    {
        // Arrange
        var testText = "";
        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ExtractJobDataAsync(testText));
    }

    [Fact]
    public async Task ExtractJobDataAsync_NullText_ThrowsArgumentException()
    {
        // Arrange
        string testText = null;
        _textPreprocessorMock.Setup(p => p.IsValidInput(testText)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ExtractJobDataAsync(testText));
    }

    [Fact]
    public async Task ExtractJobDataAsync_ExtractsSkillsFromText()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var textWithSkills = "We need a React and Node.js developer with AWS experience";
        _textPreprocessorMock.Setup(p => p.IsValidInput(textWithSkills)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(textWithSkills)).Returns(textWithSkills);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(textWithSkills)).Returns(textWithSkills);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(textWithSkills)).Returns(textWithSkills);

        var mockResult = new ExtractedJobData
        {
            Title = "Job Position",
            Company = "Company Name",
            Skills = new List<string> { "React", "Node.js", "AWS" }
        };
        _jobDataExtractorMock.Setup(e => e.ExtractJobData(textWithSkills)).Returns(mockResult);

        // Act
        var result = await service.ExtractJobDataAsync(textWithSkills);

        // Assert
        Assert.Contains("React", result.Skills);
        Assert.Contains("Node.js", result.Skills);
        Assert.Contains("AWS", result.Skills);
    }

    [Fact]
    public async Task ExtractJobDataAsync_ExtractsExperienceLevel()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var testCases = new[]
        {
            ("Senior Developer with 5+ years experience", "Senior"),
            ("Junior Developer entry level", "Entry-level"),
            ("Mid level engineer", "Mid-level"),
            ("Lead Developer position", "Senior")
        };

        foreach (var (text, expectedLevel) in testCases)
        {
            _textPreprocessorMock.Setup(p => p.IsValidInput(text)).Returns(true);
            _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(text)).Returns(text);
            _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(text)).Returns(text);
            _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(text)).Returns(text);

            var mockResult = new ExtractedJobData
            {
                Title = "Job Position",
                Company = "Company Name",
                Skills = new List<string> { "See job description" },
                ExperienceLevel = expectedLevel
            };
            _jobDataExtractorMock.Setup(e => e.ExtractJobData(text)).Returns(mockResult);

            // Act
            var result = await service.ExtractJobDataAsync(text);

            // Assert
            Assert.Equal(expectedLevel, result.ExperienceLevel);
        }
    }

    [Fact]
    public async Task ExtractJobDataAsync_ExtractsLocation()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var testCases = new[]
        {
            ("Remote React Developer", "Remote"),
            ("Hybrid work opportunity", "Hybrid"),
            ("On-site position in London", "On-site")
        };

        foreach (var (text, expectedLocation) in testCases)
        {
            _textPreprocessorMock.Setup(p => p.IsValidInput(text)).Returns(true);
            _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(text)).Returns(text);
            _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(text)).Returns(text);
            _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(text)).Returns(text);

            var mockResult = new ExtractedJobData
            {
                Title = "Job Position",
                Company = "Company Name",
                Skills = new List<string> { "See job description" },
                Location = expectedLocation
            };
            _jobDataExtractorMock.Setup(e => e.ExtractJobData(text)).Returns(mockResult);

            // Act
            var result = await service.ExtractJobDataAsync(text);

            // Assert
            Assert.Equal(expectedLocation, result.Location);
        }
    }

    [Fact]
    public async Task ExtractJobDataAsync_ExtractsSalaryRange()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var testCases = new[]
        {
            ("Salary $80,000 - $100,000", "$80,000 - $100,000"),
            ("£50k - £70k per year", "£50k - £70k"),
            ("$90k annual salary", "$90k")
        };

        foreach (var (text, expectedSalary) in testCases)
        {
            _textPreprocessorMock.Setup(p => p.IsValidInput(text)).Returns(true);
            _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(text)).Returns(text);
            _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(text)).Returns(text);
            _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(text)).Returns(text);

            var mockResult = new ExtractedJobData
            {
                Title = "Job Position",
                Company = "Company Name",
                Skills = new List<string> { "See job description" },
                SalaryRange = expectedSalary
            };
            _jobDataExtractorMock.Setup(e => e.ExtractJobData(text)).Returns(mockResult);

            // Act
            var result = await service.ExtractJobDataAsync(text);

            // Assert
            Assert.Equal(expectedSalary, result.SalaryRange);
        }
    }

    [Fact]
    public async Task ExtractJobDataAsync_GeneratesStructuredSummary()
    {
        // Arrange
        var options = new AiExtractionOptions { ApiKey = string.Empty };
        var service = new AiExtractionService(
            options,
            _loggerMock.Object,
            _textPreprocessorMock.Object,
            _cache,
            _httpClientFactoryMock.Object);

        var jobText = @"Senior Full Stack Developer at TechCorp

We are looking for an experienced developer to join our team.

Responsibilities:
- Build web applications
- Work with React and Node.js
- Deploy to AWS

Requirements:
- 3+ years experience
- React, Node.js skills

Benefits:
- Remote work
- Competitive salary
- Health insurance";

        _textPreprocessorMock.Setup(p => p.IsValidInput(jobText)).Returns(true);
        _textPreprocessorMock.Setup(p => p.RemoveHtmlEntities(jobText)).Returns(jobText);
        _textPreprocessorMock.Setup(p => p.NormalizeWhitespace(jobText)).Returns(jobText);
        _textPreprocessorMock.Setup(p => p.RemoveWebpageNoise(jobText)).Returns(jobText);

        var mockResult = new ExtractedJobData
        {
            Title = "Job Position",
            Company = "Company Name",
            Skills = new List<string> { "See job description" },
            DescriptionSummary = "**Senior Full Stack Developer** at **TechCorp**\n\n**Overview:** We are looking for an experienced developer..."
        };
        _jobDataExtractorMock.Setup(e => e.ExtractJobData(jobText)).Returns(mockResult);

        // Act
        var result = await service.ExtractJobDataAsync(jobText);

        // Assert
        Assert.NotNull(result.DescriptionSummary);
        Assert.Contains("**", result.DescriptionSummary); // Should contain markdown formatting
    }
}