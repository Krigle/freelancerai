using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FreelanceFinderAI.Models;
using FreelanceFinderAI.Services;
using FreelanceFinderAI.DTOs;
using FreelanceFinderAI.Data;

namespace FreelanceFinderAI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AiExtractionService _aiService;
    private readonly AppDbContext _context;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        AiExtractionService aiService, 
        AppDbContext context,
        ILogger<JobsController> logger)
    {
        _aiService = aiService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeJob([FromBody] JobRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.JobText))
        {
            return BadRequest(new { error = "Job text cannot be empty" });
        }

        try
        {
            _logger.LogInformation("Analyzing job posting...");
            var extractedData = await _aiService.ExtractJobDataAsync(request.JobText);
            
            var job = new Job
            {
                OriginalText = request.JobText,
                Extracted = extractedData,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Job analyzed and saved with ID: {job.Id}");
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing job");
            return StatusCode(500, new { error = "Failed to analyze job posting" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        try
        {
            var jobs = await _context.Jobs
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs");
            return StatusCode(500, new { error = "Failed to retrieve jobs" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(int id)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(id);
            
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }
            
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving job {id}");
            return StatusCode(500, new { error = "Failed to retrieve job" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(id);
            
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }
            
            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting job {id}");
            return StatusCode(500, new { error = "Failed to delete job" });
        }
    }
}

