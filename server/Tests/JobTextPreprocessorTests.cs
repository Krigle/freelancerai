using FreelanceFinderAI.Services;
using Xunit;

namespace FreelanceFinderAI.Tests;

public class JobTextPreprocessorTests
{
    private readonly JobTextPreprocessor _preprocessor;

    public JobTextPreprocessorTests()
    {
        _preprocessor = new JobTextPreprocessor();
    }

    [Theory]
    [InlineData("Hello & World", "Hello & World")]
    [InlineData("Price: &pound;100", "Price: £100")]
    [InlineData("Normal text", "Normal text")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void RemoveHtmlEntities_HandlesVariousInputs(string input, string expected)
    {
        // Act
        var result = _preprocessor.RemoveHtmlEntities(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello   World", "Hello World")]
    [InlineData("Line1\n\n\nLine2", "Line1\n\nLine2")]
    [InlineData("Normal text", "Normal text")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void NormalizeWhitespace_HandlesVariousInputs(string input, string expected)
    {
        // Act
        var result = _preprocessor.NormalizeWhitespace(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveWebpageNoise_RemovesScriptTags()
    {
        // Arrange
        var input = @"<html><body>
<script>alert('test');</script>
<p>Important content</p>
<script src='external.js'></script>
</body></html>";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("Important content", result);
    }

    [Fact]
    public void RemoveWebpageNoise_RemovesStyleTags()
    {
        // Arrange
        var input = @"<html><head>
<style>body { color: red; }</style>
</head><body>
<p>Important content</p>
</body></html>";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.DoesNotContain("<style>", result);
        Assert.Contains("Important content", result);
    }

    [Fact]
    public void RemoveWebpageNoise_RemovesHtmlComments()
    {
        // Arrange
        var input = @"<!-- Header comment -->
<p>Important content</p>
<!-- Footer comment -->";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.DoesNotContain("<!--", result);
        Assert.Contains("Important content", result);
    }

    [Fact]
    public void RemoveWebpageNoise_RemovesNavigationElements()
    {
        // Arrange
        var input = @"<nav>Home | About | Contact</nav>
<p>Important content</p>
<nav>Footer nav</nav>";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.DoesNotContain("<nav>", result);
        Assert.Contains("Important content", result);
    }

    [Fact]
    public void RemoveWebpageNoise_RemovesFooterElements()
    {
        // Arrange
        var input = @"<p>Important content</p>
<footer>Copyright 2024</footer>
<p>More content</p>";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.DoesNotContain("<footer>", result);
        Assert.Contains("Important content", result);
        Assert.Contains("More content", result);
    }

    [Fact]
    public void RemoveWebpageNoise_RemovesHeaderElements()
    {
        // Arrange
        var input = @"<header>Site Header</header>
<p>Important content</p>";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.DoesNotContain("<header>", result);
        Assert.Contains("Important content", result);
    }

    [Fact]
    public void RemoveWebpageNoise_RemovesAdvertisementElements()
    {
        // Arrange
        var input = @"<p>Important content</p>
<div class='ad'>Advertisement</div>
<p>More content</p>";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.DoesNotContain("Advertisement", result);
        Assert.Contains("Important content", result);
        Assert.Contains("More content", result);
    }

    [Fact]
    public void RemoveWebpageNoise_NormalizesExcessiveLineBreaks()
    {
        // Arrange
        var input = "Line1\n\n\n\n\n\nLine2";

        // Act
        var result = _preprocessor.RemoveWebpageNoise(input);

        // Assert
        Assert.Equal("Line1\n\nLine2", result);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Short", false)]
    [InlineData("This is a reasonable length job description with enough words to be valid.", true)]
    public void IsValidInput_ValidatesInputCorrectly(string input, bool expected)
    {
        // Act
        var result = _preprocessor.IsValidInput(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidInput_RejectsExcessiveSpecialCharacters()
    {
        // Arrange
        var input = new string('!', 60); // More than 50% special chars

        // Act
        var result = _preprocessor.IsValidInput(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidInput_RejectsTooFewWords()
    {
        // Arrange
        var input = "Word1 Word2 Word3"; // Only 3 words

        // Act
        var result = _preprocessor.IsValidInput(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidInput_AcceptsValidInput()
    {
        // Arrange
        var input = "We are looking for a software developer with experience in React and Node.js. The position requires at least 3 years of experience.";

        // Act
        var result = _preprocessor.IsValidInput(input);

        // Assert
        Assert.True(result);
    }
}