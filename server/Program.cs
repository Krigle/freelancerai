using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Polly;
using Polly.Timeout;
using FreelanceFinderAI.Data;
using FreelanceFinderAI.Models;
using FreelanceFinderAI.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ASPNETCORE_URLS environment variable will handle the binding
        // Default: http://+:5001 (all interfaces) in production
        // Can be overridden via environment variable

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add DbContext with SQLite - prioritize environment variable
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=freelancefinder.db";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Add memory cache
        builder.Services.AddMemoryCache();

        // Configure HttpClient with resilience policies
        builder.Services.AddHttpClient("AiApiClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30); // Overall timeout
        })
        .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
        .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10))); // Per-request timeout

        // Bind AiExtractionOptions configuration - prioritize environment variables
        var aiOptions = new AiExtractionOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                ?? builder.Configuration["OpenAI:ApiKey"]
                ?? string.Empty,
            BaseUrl = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL")
                ?? builder.Configuration["OpenAI:BaseUrl"]
                ?? "https://openrouter.ai/api/v1",
            Model = Environment.GetEnvironmentVariable("OPENROUTER_MODEL")
                ?? builder.Configuration["OpenAI:Model"]
                ?? "openai/gpt-4o-mini"
        };
        builder.Services.AddSingleton(aiOptions);

        // Add AI Extraction Service
        builder.Services.AddScoped<IJobTextPreprocessor, JobTextPreprocessor>();
        builder.Services.AddScoped<AiExtractionService>();

        // Add CORS - prioritize environment variable
        var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:5174" };

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        // Ensure database is created
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowFrontend");

        // Serve static files (React frontend) in production
        if (!app.Environment.IsDevelopment())
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        app.UseAuthorization();

        // Health check endpoint for Coolify
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        app.MapControllers();

        // Fallback to index.html for SPA routing in production
        if (!app.Environment.IsDevelopment())
        {
            app.MapFallbackToFile("index.html");
        }

        app.Run();
    }

}
