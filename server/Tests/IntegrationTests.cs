using System.Net;
using System.Net.Http.Json;
using System.Text;
using FreelanceFinderAI.Controllers;
using FreelanceFinderAI.Data;
using FreelanceFinderAI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace FreelanceFinderAI.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the database with an in-memory one for testing
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FullJobSubmissionFlow_WorksEndToEnd()
    {
        // Arrange
        var jobText = @"Senior React Developer at TechCorp

We are looking for a Senior React Developer to join our remote team.

Requirements:
- 5+ years React experience
- TypeScript proficiency
- Node.js backend experience

Benefits:
- Fully remote
- $100k - $130k salary
- Health insurance
- Flexible hours

Apply now!";

        var request = new
        {
            text = jobText
        };

        // Act - Submit job
        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        // Assert - Job created successfully
        Assert.True(response.IsSuccessStatusCode);

        var createdJob = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(createdJob);
        Assert.True(createdJob.Id > 0);
        Assert.Equal(jobText, createdJob.OriginalText);

        // Wait a bit for AI processing (in real scenario, this might be async)
        await Task.Delay(100);

        // Act - Get the job back
        var getResponse = await _client.GetAsync($"/api/jobs/{createdJob.Id}");

        // Assert - Job retrieved with extracted data
        Assert.True(getResponse.IsSuccessStatusCode);

        var retrievedJob = await getResponse.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(retrievedJob);
        Assert.NotNull(retrievedJob.Extracted);
        Assert.Equal(createdJob.Id, retrievedJob.Id);
    }

    [Fact]
    public async Task JobSubmission_WithEmptyText_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            text = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task JobSubmission_WithNullText_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            text = (string)null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNonExistentJob_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/jobs/99999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteJob_RemovesJobSuccessfully()
    {
        // Arrange - Create a job first
        var jobText = "Test job for deletion";
        var createRequest = new { text = jobText };
        var createResponse = await _client.PostAsJsonAsync("/api/jobs", createRequest);
        var createdJob = await createResponse.Content.ReadFromJsonAsync<Job>();

        // Act - Delete the job
        var deleteResponse = await _client.DeleteAsync($"/api/jobs/{createdJob.Id}");

        // Assert - Delete successful
        Assert.True(deleteResponse.IsSuccessStatusCode);

        // Verify job is gone
        var getResponse = await _client.GetAsync($"/api/jobs/{createdJob.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAllJobs_ReturnsJobsList()
    {
        // Arrange - Create multiple jobs
        var jobs = new[]
        {
            "First test job",
            "Second test job",
            "Third test job"
        };

        foreach (var jobText in jobs)
        {
            var request = new { text = jobText };
            await _client.PostAsJsonAsync("/api/jobs", request);
        }

        // Act
        var response = await _client.GetAsync("/api/jobs");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var retrievedJobs = await response.Content.ReadFromJsonAsync<List<Job>>();
        Assert.NotNull(retrievedJobs);
        Assert.True(retrievedJobs.Count >= 3); // At least the jobs we created
    }

    [Fact]
    public async Task JobWithLongText_HandlesGracefully()
    {
        // Arrange
        var longText = string.Join(" ", Enumerable.Repeat("This is a very long job description that should be handled properly by the system.", 50));

        var request = new { text = longText };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var job = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(job);
        Assert.Equal(longText, job.OriginalText);
    }

    [Fact]
    public async Task JobWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var textWithSpecialChars = "Job with special chars: àáâãäå, 中文, русский, $100k-150k, @company.com";

        var request = new { text = textWithSpecialChars };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var job = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(job);
        Assert.Equal(textWithSpecialChars, job.OriginalText);
    }

    [Fact]
    public async Task ConcurrentJobSubmissions_HandledCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            var request = new { text = $"Concurrent job {i}" };
            tasks.Add(_client.PostAsJsonAsync("/api/jobs", request));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            Assert.True(response.IsSuccessStatusCode);
        }

        // Verify all jobs were created
        var getAllResponse = await _client.GetAsync("/api/jobs");
        var jobs = await getAllResponse.Content.ReadFromJsonAsync<List<Job>>();
        Assert.True(jobs.Count >= 5);
    }

    [Fact]
    public async Task JobExtraction_ProducesStructuredData()
    {
        // Arrange
        var jobText = @"Senior Full Stack Developer
TechCorp Inc.

Location: Remote
Salary: $120,000 - $150,000
Experience: 5+ years

We need a senior developer with React, Node.js, and AWS skills.

Responsibilities:
- Build scalable web applications
- Lead development team
- Mentor junior developers

Requirements:
- React expertise
- Node.js proficiency
- AWS cloud experience
- 5+ years experience

Benefits:
- 100% remote work
- Competitive salary
- Stock options
- Health/dental/vision insurance";

        var request = new { text = jobText };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var job = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(job);
        Assert.NotNull(job.Extracted);

        // Verify structured data extraction
        var extracted = job.Extracted;
        Assert.NotNull(extracted.Title);
        Assert.NotNull(extracted.Company);
        Assert.NotNull(extracted.Skills);
        Assert.NotNull(extracted.DescriptionSummary);

        // Summary should be structured with sections
        Assert.Contains("**", extracted.DescriptionSummary);
        Assert.True(extracted.DescriptionSummary.Length <= 800);
    }

    [Fact]
    public async Task AiExtractionService_HandlesApiErrorsGracefully()
    {
        // Arrange - Create a custom factory that mocks HTTP calls
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDb"));

                // Mock HTTP client for AI service
                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent("API Error")
                    });

                var httpClient = new HttpClient(handlerMock.Object);
                services.AddHttpClient("OpenAI", client => client = httpClient);
            });
        });

        var client = customFactory.CreateClient();

        var jobText = @"Senior Developer
Test Company

We need a developer with React skills.";

        var request = new { text = jobText };

        // Act
        var response = await client.PostAsJsonAsync("/api/jobs", request);

        // Assert - Should still succeed with fallback to mock extraction
        Assert.True(response.IsSuccessStatusCode);

        var job = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(job);
        Assert.NotNull(job.Extracted);
        Assert.Equal("Senior Developer", job.Extracted.Title);
        Assert.Equal("Test Company", job.Extracted.Company);
    }

    [Fact]
    public async Task AiExtractionService_HandlesTimeoutGracefully()
    {
        // Arrange
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDb"));

                // Mock HTTP client that times out
                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>(async (request, token) =>
                    {
                        await Task.Delay(5000, token); // Longer than timeout
                        return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
                    });

                var httpClient = new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(1) };
                services.AddHttpClient("OpenAI", client => client = httpClient);
            });
        });

        var client = customFactory.CreateClient();

        var jobText = @"Developer Position
Company Name

Looking for a developer.";

        var request = new { text = jobText };

        // Act
        var response = await client.PostAsJsonAsync("/api/jobs", request);

        // Assert - Should fallback to mock extraction
        Assert.True(response.IsSuccessStatusCode);

        var job = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(job);
        Assert.NotNull(job.Extracted);
    }

    [Fact]
    public async Task AiExtractionService_HandlesMalformedApiResponse()
    {
        // Arrange
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDb"));

                // Mock HTTP client with malformed JSON response
                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
                    });

                var httpClient = new HttpClient(handlerMock.Object);
                services.AddHttpClient("OpenAI", client => client = httpClient);
            });
        });

        var client = customFactory.CreateClient();

        var jobText = @"Developer
Company

Job description.";

        var request = new { text = jobText };

        // Act
        var response = await client.PostAsJsonAsync("/api/jobs", request);

        // Assert - Should fallback to mock extraction
        Assert.True(response.IsSuccessStatusCode);

        var job = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(job);
        Assert.NotNull(job.Extracted);
    }

    [Fact]
    public async Task AiExtractionService_HandlesRateLimiting()
    {
        // Arrange
        var callCount = 0;
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDb"));

                // Mock HTTP client that returns rate limit error on first call, success on retry
                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>((request, token) =>
                    {
                        callCount++;
                        if (callCount == 1)
                        {
                            return Task.FromResult(new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.TooManyRequests,
                                Content = new StringContent("Rate limit exceeded")
                            });
                        }
                        else
                        {
                            // Return successful response with valid JSON
                            var responseContent = @"{
                                ""choices"": [{
                                    ""message"": {
                                        ""content"": ""{\""title\"": \""Developer\"", \""company\"": \""Company\"", \""skills\"": [\""React\""], \""experienceLevel\"": \""Mid-level\"", \""location\"": \""Remote\"", \""salaryRange\"": \""$100k\"", \""descriptionSummary\"": \""Developer at Company\""}""
                                    }
                                }]
                            }";
                            return Task.FromResult(new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                            });
                        }
                    });

                var httpClient = new HttpClient(handlerMock.Object);
                services.AddHttpClient("OpenAI", client => client = httpClient);
            });
        });

        var client = customFactory.CreateClient();

        var jobText = @"Developer
Company

Job description.";

        var request = new { text = jobText };

        // Act
        var response = await client.PostAsJsonAsync("/api/jobs", request);

        // Assert - Should eventually succeed after retry
        Assert.True(response.IsSuccessStatusCode);

        var job = await response.Content.ReadFromJsonAsync<Job>();
        Assert.NotNull(job);
        Assert.NotNull(job.Extracted);
        Assert.Equal("Developer", job.Extracted.Title);
        Assert.Equal("Company", job.Extracted.Company);
        Assert.Contains("React", job.Extracted.Skills);
    }
}