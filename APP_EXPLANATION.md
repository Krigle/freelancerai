# FreelanceFinderAI Application Architecture & Data Flow

## Overview

FreelanceFinderAI is a full-stack web application that leverages AI to analyze and extract structured data from freelance job postings. Users can paste job listings, and the app uses OpenAI's API to intelligently parse key information like job title, company, skills, experience level, location, salary, and description.

## Architecture Overview

The application follows a modern full-stack architecture:

- **Backend**: C# .NET 9 Web API with Entity Framework Core and SQLite database
- **Frontend**: React 18 with TypeScript, Vite for development, Tailwind CSS for styling
- **AI Integration**: OpenAI API (via OpenRouter) for intelligent data extraction
- **Database**: SQLite for data persistence
- **Deployment**: Docker containerization with Docker Compose

## Data Flow Architecture

### 1. User Interaction (Frontend)

The data flow begins when a user pastes a job listing into the React frontend:

```typescript
// client/src/components/JobInputForm.tsx
const handleSubmit = async (e: React.FormEvent) => {
  // User submits job text
  const job = await analyzeJob(text); // API call to backend
  onJobAnalyzed(job); // Update UI state
};
```

### 2. API Request (Client to Server)

The frontend makes an HTTP POST request to the backend API:

```typescript
// client/src/api/jobs.ts
export async function analyzeJob(jobText: string): Promise<Job> {
  const response = await fetch(`${API_BASE_URL}/jobs/analyze`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ jobText } as JobRequestDto),
  });
  return response.json();
}
```

### 3. Backend Processing (C# Web API)

#### Program.cs - Application Startup

Similar to how Express.js uses `app.listen()` and middleware, .NET uses `WebApplication.CreateBuilder()` and service registration:

```csharp
// server/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register services (like Express middleware)
builder.Services.AddControllers(); // Enables MVC controllers
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AiExtractionService>(); // Dependency injection

// Configure CORS (like Express cors middleware)
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Middleware pipeline (similar to Express app.use())
app.UseCors("AllowFrontend");
app.MapControllers(); // Routes controllers (like Express app.use('/api', router))
app.Run();
```

#### JobsController.cs - API Endpoints

The controller handles HTTP requests, similar to Express route handlers:

```csharp
// server/Controllers/JobsController.cs
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AiExtractionService _aiService;
    private readonly AppDbContext _context;

    // Constructor injection (like Express middleware injection)
    public JobsController(AiExtractionService aiService, AppDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeJob([FromBody] JobRequestDto request)
    {
        // 1. Extract data using AI service
        var extractedData = await _aiService.ExtractJobDataAsync(request.JobText);

        // 2. Create job entity
        var job = new Job
        {
            OriginalText = request.JobText,
            Extracted = extractedData,
            CreatedAt = DateTime.UtcNow
        };

        // 3. Save to database
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return Ok(job);
    }
}
```

### 4. AI Data Extraction Service

The `AiExtractionService` handles the AI-powered data extraction:

```csharp
// server/Services/AiExtractionService.cs
public class AiExtractionService
{
    public async Task<ExtractedJobData> ExtractJobDataAsync(string text)
    {
        // Build prompt for AI
        var prompt = BuildExtractionPrompt(text);

        // Call OpenAI API
        var payload = new {
            model = _model,
            messages = new[] {
                new { role = "system", content = "Extract structured information..." },
                new { role = "user", content = prompt }
            }
        };

        // Make HTTP request to OpenAI
        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        // Parse JSON response and extract structured data
        var extractedData = JsonSerializer.Deserialize<ExtractedJobData>(json);
        return extractedData;
    }
}
```

### 5. Database Layer (Entity Framework Core)

#### Data Models

The app uses Entity Framework Core (EF Core) for ORM, similar to how you'd use Mongoose with MongoDB or Sequelize with SQL:

```csharp
// server/Models/Job.cs
public class Job
{
    public int Id { get; set; }
    public string OriginalText { get; set; } = string.Empty;

    // JSON storage for extracted data
    [Column(TypeName = "TEXT")]
    public string ExtractedJson { get; set; } = string.Empty;

    // Computed property (not stored in DB)
    [NotMapped]
    public ExtractedJobData? Extracted
    {
        get => JsonSerializer.Deserialize<ExtractedJobData>(ExtractedJson);
        set => ExtractedJson = JsonSerializer.Serialize(value);
    }

    public DateTime CreatedAt { get; set; }
}

// server/Models/ExtractedJobData.cs
public class ExtractedJobData
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string ExperienceLevel { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string SalaryRange { get; set; } = string.Empty;
    public string DescriptionSummary { get; set; } = string.Empty;
}
```

#### Database Context

The `AppDbContext` manages database connections and entity configurations:

```csharp
// server/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Job> Jobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalText).IsRequired();
            entity.Property(e => e.ExtractedJson).IsRequired();
        });
    }
}
```

### 6. Frontend State Management & Display

#### React Components

The frontend uses React hooks for state management:

```typescript
// client/src/App.tsx
function App() {
  const [refreshKey, setRefreshKey] = useState(0);

  const handleJobAnalyzed = (job: Job) => {
    // Trigger dashboard refresh
    setRefreshKey((prev) => prev + 1);
  };

  return (
    <div>
      <JobInputForm onJobAnalyzed={handleJobAnalyzed} />
      <Dashboard key={refreshKey} /> {/* Key change forces re-render */}
    </div>
  );
}
```

#### Dashboard Component

Fetches and displays jobs from the API:

```typescript
// client/src/components/Dashboard.tsx
export default function Dashboard() {
  const [jobs, setJobs] = useState<Job[]>([]);

  const loadJobs = async () => {
    const data = await getJobs(); // API call
    setJobs(data);
  };

  useEffect(() => {
    loadJobs();
  }, []);
}
```

## Key Differences from Express.js

### 1. Dependency Injection

In Express.js, you manually pass dependencies:

```javascript
// Express.js
const jobService = new JobService();
app.post("/analyze", (req, res) => jobService.analyze(req, res));
```

In .NET, dependencies are injected automatically:

```csharp
// .NET
public JobsController(AiExtractionService aiService) {
    _aiService = aiService; // Injected by DI container
}
```

### 2. ORM vs Manual Queries

Express.js often requires manual SQL queries or ORM setup:

```javascript
// Express.js with manual queries
const result = await db.query("SELECT * FROM jobs WHERE id = ?", [id]);
```

EF Core handles this automatically:

```csharp
// .NET EF Core
var job = await _context.Jobs.FindAsync(id);
```

### 3. Middleware Pipeline

Express.js uses a linear middleware chain:

```javascript
// Express.js
app.use(cors());
app.use(express.json());
app.use("/api", routes);
```

.NET uses a builder pattern with service registration:

```csharp
// .NET
builder.Services.AddCors();
builder.Services.AddControllers();
var app = builder.Build();
app.UseCors();
app.MapControllers();
```

### 4. Async/Await Patterns

Both frameworks support async operations, but .NET uses `Task<T>`:

```csharp
// .NET
public async Task<IActionResult> AnalyzeJob([FromBody] JobRequestDto request) {
    var extractedData = await _aiService.ExtractJobDataAsync(request.JobText);
    // ...
}
```

## Database Schema & Relationships

The database uses a simple schema with one main table:

```sql
-- Jobs table (SQLite)
CREATE TABLE Jobs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OriginalText TEXT NOT NULL,
    ExtractedJson TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL
);
```

The `ExtractedJson` field stores the AI-extracted data as JSON, which gets deserialized into the `ExtractedJobData` object when accessed.

## Configuration & Environment

### appsettings.json

Configuration is stored in JSON files:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=freelancefinder.db"
  },
  "OpenAI": {
    "ApiKey": "",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-4o-mini"
  }
}
```

### Environment Variables

Sensitive data like API keys are managed through environment variables and Docker Compose:

```yaml
# docker-compose.yml
environment:
  - OpenAI__ApiKey=${OPENAI_API_KEY:-}
```

## Error Handling & Logging

The application implements comprehensive error handling:

- **Backend**: Try-catch blocks with logging using `ILogger`
- **Frontend**: Error states in React components with user-friendly messages
- **API**: HTTP status codes and structured error responses

## Deployment & Docker

The app uses Docker for containerization:

```yaml
# docker-compose.yml
services:
  server:
    build: ./server
    ports: ["5000:80"]
  client:
    build: ./client
    ports: ["3000:80"]
```

## API Endpoints

| Method | Endpoint            | Description                 |
| ------ | ------------------- | --------------------------- |
| POST   | `/api/jobs/analyze` | Analyze job posting with AI |
| GET    | `/api/jobs`         | Get all analyzed jobs       |
| GET    | `/api/jobs/{id}`    | Get specific job by ID      |
| DELETE | `/api/jobs/{id}`    | Delete a job                |

## Data Types

### Frontend Types (TypeScript)

```typescript
interface Job {
  id: number;
  originalText: string;
  extractedJson: string;
  extracted: ExtractedJobData | null;
  createdAt: string;
}

interface ExtractedJobData {
  title: string;
  company: string;
  skills: string[];
  experienceLevel: string;
  location: string;
  salaryRange: string;
  descriptionSummary: string;
}
```

### Backend Models (C#)

```csharp
public class Job {
    public int Id { get; set; }
    public string OriginalText { get; set; }
    public string ExtractedJson { get; set; }
    public ExtractedJobData? Extracted { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Summary

This application demonstrates a complete full-stack implementation with:

1. **Client-Server Communication**: RESTful API calls between React frontend and C# backend
2. **AI Integration**: OpenAI API for intelligent data extraction with fallback handling
3. **Database Persistence**: SQLite with EF Core for data storage and retrieval
4. **Modern C# Patterns**: Dependency injection, async/await, LINQ queries
5. **React Best Practices**: Hooks, TypeScript, component composition
6. **Production Ready**: Docker deployment, error handling, logging, CORS configuration

The architecture provides a solid foundation for understanding how modern .NET applications work compared to Node.js/Express.js, with strong typing, dependency injection, and ORM abstractions that simplify development while maintaining performance and scalability.
