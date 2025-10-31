using FreelanceFinderAI.Services;
using Xunit;

namespace FreelanceFinderAI.Tests;

public class JobSummaryGeneratorTests
{
    private readonly JobSummaryGenerator _generator;

    public JobSummaryGeneratorTests()
    {
        _generator = new JobSummaryGenerator();
    }

    [Fact]
    public void GenerateStructuredSummary_IncludesTitleAndCompany()
    {
        // Arrange
        var text = "Job description text";
        var title = "Senior Developer";
        var company = "TechCorp";
        var experienceLevel = "Senior";
        var location = "Remote";
        var salaryRange = "$100k-120k";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("**Senior Developer** at **TechCorp**", result);
    }

    [Fact]
    public void GenerateStructuredSummary_IncludesLocationDetails()
    {
        // Arrange
        var text = "Job description text";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Mid-level";
        var location = "Remote";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("📍 Remote", result);
    }

    [Fact]
    public void GenerateStructuredSummary_IncludesExperienceLevel()
    {
        // Arrange
        var text = "Job description text";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Senior";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("👤 Senior", result);
    }

    [Fact]
    public void GenerateStructuredSummary_IncludesSalaryRange()
    {
        // Arrange
        var text = "Job description text";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "$80k-100k";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("💰 $80k-100k", result);
    }

    [Fact]
    public void GenerateStructuredSummary_CombinesMultipleDetails()
    {
        // Arrange
        var text = "Job description text";
        var title = "Senior Developer";
        var company = "TechCorp";
        var experienceLevel = "Senior";
        var location = "Remote";
        var salaryRange = "$100k-120k";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("📍 Remote", result);
        Assert.Contains("👤 Senior", result);
        Assert.Contains("💰 $100k-120k", result);
    }

    [Fact]
    public void GenerateStructuredSummary_ExcludesNotSpecifiedValues()
    {
        // Arrange
        var text = "Job description text";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.DoesNotContain("📍", result);
        Assert.DoesNotContain("👤", result);
        Assert.DoesNotContain("💰", result);
    }

    [Fact]
    public void GenerateStructuredSummary_ExtractsCompanyDescription()
    {
        // Arrange
        var text = @"TechCorp is a leading technology company that specializes in innovative solutions.
We are looking for talented developers to join our team.";
        var title = "Developer";
        var company = "TechCorp";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("**Overview:**", result);
        Assert.Contains("leading technology company", result);
    }

    [Fact]
    public void GenerateStructuredSummary_ExtractsResponsibilities()
    {
        // Arrange
        var text = @"Responsibilities:
- Build web applications
- Work with React and Node.js
- Deploy to AWS
- Mentor junior developers";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("**Key Responsibilities:**", result);
        Assert.Contains("• Build web applications", result);
        Assert.Contains("• Work with React and Node.js", result);
    }

    [Fact]
    public void GenerateStructuredSummary_ExtractsRequirements()
    {
        // Arrange
        var text = @"Requirements:
- 3+ years React experience
- Node.js proficiency
- AWS cloud experience
- Bachelor's degree in Computer Science";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("**Requirements:**", result);
        Assert.Contains("• 3+ years React experience", result);
        Assert.Contains("• Node.js proficiency", result);
    }

    [Fact]
    public void GenerateStructuredSummary_ExtractsBenefits()
    {
        // Arrange
        var text = @"Benefits:
- Competitive salary
- Health insurance
- Flexible work hours
- Professional development budget";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("**Benefits:**", result);
        Assert.Contains("• Competitive salary", result);
        Assert.Contains("• Health insurance", result);
    }

    [Fact]
    public void GenerateStructuredSummary_LimitsBulletPoints()
    {
        // Arrange
        var text = @"Responsibilities:
- Task 1
- Task 2
- Task 3
- Task 4
- Task 5
- Task 6";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        var bulletCount = result.Split('•').Length - 1; // Count bullet points
        Assert.Equal(4, bulletCount); // Should limit to 4 bullets for responsibilities
    }

    [Fact]
    public void GenerateStructuredSummary_RemovesWebpageNoise()
    {
        // Arrange
        var text = @"<script>alert('test');</script>
<!-- Comment -->
<nav>Navigation</nav>
<footer>Footer content</footer>
Full job description
Important job content here.
Hiring Lab
Career advice";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.DoesNotContain("<!--", result);
        Assert.DoesNotContain("<nav>", result);
        Assert.DoesNotContain("<footer>", result);
        Assert.DoesNotContain("Hiring Lab", result);
        Assert.Contains("Important job content here", result);
    }

    [Fact]
    public void GenerateStructuredSummary_HandlesEmptyText()
    {
        // Arrange
        var text = "";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("**Developer** at **Company**", result);
        Assert.NotNull(result);
    }

    [Fact]
    public void GenerateStructuredSummary_FiltersOutInvalidBulletPoints()
    {
        // Arrange
        var text = @"Responsibilities:
- Valid task here
# Hashtag content
- Another valid task
- we are proud to offer
- Valid task three
@company.com email";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("• Valid task here", result);
        Assert.Contains("• Another valid task", result);
        Assert.Contains("• Valid task three", result);
        Assert.DoesNotContain("#", result);
        Assert.DoesNotContain("@company.com", result);
        Assert.DoesNotContain("we are proud", result);
    }

    [Fact]
    public void GenerateStructuredSummary_UsesMarkdownFormatting()
    {
        // Arrange
        var text = "Simple job description";
        var title = "Developer";
        var company = "Company";
        var experienceLevel = "Not specified";
        var location = "Not specified";
        var salaryRange = "Not specified";

        // Act
        var result = _generator.GenerateStructuredSummary(text, title, company, experienceLevel, location, salaryRange);

        // Assert
        Assert.Contains("**", result); // Should contain markdown bold formatting
    }
}