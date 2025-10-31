using FreelanceFinderAI.Models;
using FreelanceFinderAI.Services;
using Moq;
using Xunit;

namespace FreelanceFinderAI.Tests;

public class JobDataExtractorTests
{
    private readonly Mock<IJobSummaryGenerator> _summaryGeneratorMock;
    private readonly JobDataExtractor _extractor;

    public JobDataExtractorTests()
    {
        _summaryGeneratorMock = new Mock<IJobSummaryGenerator>();
        _extractor = new JobDataExtractor(_summaryGeneratorMock.Object);
    }

    [Fact]
    public void ExtractJobData_ExtractsTitleFromFirstLine()
    {
        // Arrange
        var text = @"Senior React Developer
TechCorp Inc.

We are looking for a senior developer...";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal("Senior React Developer", result.Title);
    }

    [Fact]
    public void ExtractJobData_ExtractsCompanyWithIncSuffix()
    {
        // Arrange
        var text = @"Senior Developer
TechCorp Inc.

We are a leading technology company...";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal("TechCorp Inc.", result.Company);
    }

    [Fact]
    public void ExtractJobData_ExtractsCompanyWithAtPattern()
    {
        // Arrange
        var text = @"Senior Developer at Google LLC

We are looking for talented developers...";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal("Google LLC", result.Company);
    }

    [Fact]
    public void ExtractJobData_ExtractsSkillsFromText()
    {
        // Arrange
        var text = @"Senior Developer

We need someone with React, Node.js, and AWS experience. Python skills are also welcome.";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Contains("React", result.Skills);
        Assert.Contains("Node.js", result.Skills);
        Assert.Contains("AWS", result.Skills);
        Assert.Contains("Python", result.Skills);
    }

    [Fact]
    public void ExtractJobData_ReturnsDefaultSkillsWhenNoneFound()
    {
        // Arrange
        var text = @"Generic Job Title

This is a job description without any specific technical skills mentioned.";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Single(result.Skills);
        Assert.Equal("See job description", result.Skills[0]);
    }

    [Theory]
    [InlineData("Senior Developer with 5+ years experience", "Senior")]
    [InlineData("Lead Engineer position", "Senior")]
    [InlineData("Sr. Software Developer", "Senior")]
    [InlineData("Junior Developer entry level", "Entry-level")]
    [InlineData("Mid level engineer", "Mid-level")]
    [InlineData("Developer with 3 years experience", "Mid-level")]
    [InlineData("Developer with 7 years experience", "Senior")]
    public void ExtractJobData_ExtractsExperienceLevel(string text, string expectedLevel)
    {
        // Arrange
        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal(expectedLevel, result.ExperienceLevel);
    }

    [Theory]
    [InlineData("Remote React Developer position", "Remote")]
    [InlineData("Hybrid work opportunity", "Hybrid")]
    [InlineData("On-site position in London", "On-site")]
    [InlineData("Office-based role", "On-site")]
    public void ExtractJobData_ExtractsLocation(string text, string expectedLocation)
    {
        // Arrange
        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal(expectedLocation, result.Location);
    }

    [Theory]
    [InlineData("Salary $80,000 - $100,000", "$80,000 - $100,000")]
    [InlineData("£50k - £70k per year", "£50k - £70k")]
    [InlineData("$90k annual salary", "$90k")]
    [InlineData("Compensation: $120,000 - $150,000", "$120,000 - $150,000")]
    public void ExtractJobData_ExtractsSalaryRange(string text, string expectedSalary)
    {
        // Arrange
        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal(expectedSalary, result.SalaryRange);
    }

    [Fact]
    public void ExtractJobData_GeneratesStructuredSummary()
    {
        // Arrange
        var text = @"Senior Developer
TechCorp

Remote position with competitive salary.";

        var expectedSummary = "**Senior Developer** at **TechCorp**\n\n**Overview:** Remote position with competitive salary.";
        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(text, "Senior Developer", "TechCorp", "Senior", "Remote", "Not specified"))
            .Returns(expectedSummary);

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal(expectedSummary, result.DescriptionSummary);
        _summaryGeneratorMock.Verify(s => s.GenerateStructuredSummary(text, "Senior Developer", "TechCorp", "Senior", "Remote", "Not specified"), Times.Once);
    }

    [Fact]
    public void ExtractJobData_HandlesEmptyText()
    {
        // Arrange
        var text = "";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Job Position", result.Title);
        Assert.Equal("Company Name", result.Company);
    }

    [Fact]
    public void ExtractJobData_HandlesNullText()
    {
        // Arrange
        string text = null;

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _extractor.ExtractJobData(text));
    }

    [Fact]
    public void ExtractJobData_ExtractsCompanyFromLineAfterTitle()
    {
        // Arrange
        var text = @"Senior Developer
Google Inc.

We are looking for talented developers...";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal("Google Inc.", result.Company);
    }

    [Fact]
    public void ExtractJobData_FiltersOutInvalidCompanyNames()
    {
        // Arrange
        var text = @"Senior Developer
Remote

We are looking for developers...";

        _summaryGeneratorMock.Setup(s => s.GenerateStructuredSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Structured summary");

        // Act
        var result = _extractor.ExtractJobData(text);

        // Assert
        Assert.Equal("Company Name", result.Company); // Should not extract "Remote" as company
    }
}