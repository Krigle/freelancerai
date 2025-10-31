using System.ComponentModel.DataAnnotations;

namespace FreelanceFinderAI.Models;

public class AiExtractionOptions
{
    [Required(ErrorMessage = "API Key is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "API Key must be between 1 and 500 characters")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Base URL is required")]
    [Url(ErrorMessage = "Base URL must be a valid URL")]
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    [Required(ErrorMessage = "Model is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Model must be between 1 and 100 characters")]
    public string Model { get; set; } = "gpt-4o-mini";

    [Range(0, 10, ErrorMessage = "Max retries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int Timeout { get; set; } = 30;

    [Range(1, 100000, ErrorMessage = "Max text length must be between 1 and 100,000 characters")]
    public int MaxTextLength { get; set; } = 10000;
}