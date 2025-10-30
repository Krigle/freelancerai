# Development Guide

This guide will help you understand and extend the FreelanceFinderAI project.

## 🏗️ Architecture Overview

### Backend Architecture

```
Controllers/
  └── JobsController.cs      # HTTP endpoints
       ↓
Services/
  └── AiExtractionService.cs # Business logic
       ↓
Data/
  └── AppDbContext.cs        # Database access
       ↓
Models/
  └── Job.cs                 # Data entities
```

### Frontend Architecture

```
App.tsx                      # Root component
  ↓
JobInputForm.tsx            # User input
  ↓
api/jobs.ts                 # API calls
  ↓
Dashboard.tsx               # Display results
  ↓
JobCard.tsx                 # Individual job
```

## 🔧 Common Development Tasks

### Adding a New API Endpoint

1. **Add method to Controller:**
   ```csharp
   // server/Controllers/JobsController.cs
   [HttpGet("search")]
   public async Task<IActionResult> SearchJobs([FromQuery] string query)
   {
       var jobs = await _context.Jobs
           .Where(j => j.OriginalText.Contains(query))
           .ToListAsync();
       return Ok(jobs);
   }
   ```

2. **Add API client method:**
   ```typescript
   // client/src/api/jobs.ts
   export async function searchJobs(query: string): Promise<Job[]> {
     const res = await fetch(`${API_BASE_URL}/jobs/search?query=${query}`);
     return res.json();
   }
   ```

3. **Use in component:**
   ```typescript
   const results = await searchJobs("React");
   ```

### Adding a New Field to Job Model

1. **Update ExtractedJobData:**
   ```csharp
   // server/Models/ExtractedJobData.cs
   public string Benefits { get; set; } = string.Empty;
   ```

2. **Update TypeScript type:**
   ```typescript
   // client/src/types/index.ts
   export interface ExtractedJobData {
     // ... existing fields
     benefits: string;
   }
   ```

3. **Update AI prompt:**
   ```csharp
   // server/Services/AiExtractionService.cs
   // Add "benefits" to the JSON structure in BuildExtractionPrompt
   ```

4. **Update UI:**
   ```tsx
   // client/src/components/JobCard.tsx
   {extracted.benefits && (
     <div>Benefits: {extracted.benefits}</div>
   )}
   ```

### Customizing the AI Prompt

Edit `server/Services/AiExtractionService.cs`:

```csharp
private string BuildExtractionPrompt(string jobText)
{
    return $@"Extract structured job data from the following job posting.

IMPORTANT: Return ONLY valid JSON, no markdown, no explanations.

Required format:
{{
  ""title"": ""exact job title"",
  ""company"": ""company name"",
  ""skills"": [""skill1"", ""skill2""],
  ""experienceLevel"": ""entry/mid/senior/lead"",
  ""location"": ""location or remote"",
  ""salaryRange"": ""salary range or empty string"",
  ""descriptionSummary"": ""brief 1-2 sentence summary""
}}

Job posting:
{jobText}";
}
```

**Tips for better prompts:**
- Be specific about format
- Use examples (few-shot learning)
- Specify what to do with missing data
- Request consistent formatting

### Adding a New React Component

1. **Create component file:**
   ```tsx
   // client/src/components/JobFilter.tsx
   import { useState } from 'react';

   interface JobFilterProps {
     onFilter: (level: string) => void;
   }

   export default function JobFilter({ onFilter }: JobFilterProps) {
     const [level, setLevel] = useState('');

     return (
       <select 
         value={level} 
         onChange={(e) => {
           setLevel(e.target.value);
           onFilter(e.target.value);
         }}
       >
         <option value="">All Levels</option>
         <option value="junior">Junior</option>
         <option value="mid">Mid</option>
         <option value="senior">Senior</option>
       </select>
     );
   }
   ```

2. **Use in parent component:**
   ```tsx
   import JobFilter from './components/JobFilter';

   function Dashboard() {
     const handleFilter = (level: string) => {
       // Filter logic
     };

     return (
       <div>
         <JobFilter onFilter={handleFilter} />
         {/* ... */}
       </div>
     );
   }
   ```

## 🎨 Styling with Tailwind

### Common Patterns

**Card:**
```tsx
<div className="bg-white shadow-lg rounded-2xl p-6">
  {/* content */}
</div>
```

**Button:**
```tsx
<button className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">
  Click me
</button>
```

**Input:**
```tsx
<input className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500" />
```

**Grid:**
```tsx
<div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
  {/* items */}
</div>
```

## 🗄️ Database Operations

### Adding a Migration (if using PostgreSQL)

```bash
cd server
dotnet ef migrations add AddNewField
dotnet ef database update
```

### Querying Data

```csharp
// Get all jobs
var jobs = await _context.Jobs.ToListAsync();

// Filter jobs
var seniorJobs = await _context.Jobs
    .Where(j => j.ExtractedJson.Contains("senior"))
    .ToListAsync();

// Get single job
var job = await _context.Jobs.FindAsync(id);

// Add job
_context.Jobs.Add(newJob);
await _context.SaveChangesAsync();

// Update job
job.OriginalText = "updated";
await _context.SaveChangesAsync();

// Delete job
_context.Jobs.Remove(job);
await _context.SaveChangesAsync();
```

## 🧪 Testing

### Backend Unit Test Example

```csharp
// server.Tests/JobsControllerTests.cs
public class JobsControllerTests
{
    [Fact]
    public async Task AnalyzeJob_ReturnsOk()
    {
        // Arrange
        var mockService = new Mock<AiExtractionService>();
        var controller = new JobsController(mockService.Object, context);
        
        // Act
        var result = await controller.AnalyzeJob(new JobRequestDto 
        { 
            JobText = "Test job" 
        });
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
```

### Frontend Test Example

```typescript
// client/src/components/JobCard.test.tsx
import { render, screen } from '@testing-library/react';
import JobCard from './JobCard';

test('renders job title', () => {
  const job = {
    id: 1,
    extracted: {
      title: 'Senior Developer',
      // ... other fields
    }
  };
  
  render(<JobCard job={job} onDelete={() => {}} />);
  expect(screen.getByText('Senior Developer')).toBeInTheDocument();
});
```

## 🔍 Debugging

### Backend Debugging

1. **Add breakpoints** in Visual Studio Code
2. **Run with debugger:**
   ```bash
   cd server
   dotnet run --launch-profile https
   ```

3. **Check logs:**
   ```csharp
   _logger.LogInformation("Processing job: {JobId}", job.Id);
   _logger.LogError(ex, "Failed to process job");
   ```

### Frontend Debugging

1. **Use React DevTools** browser extension
2. **Console logging:**
   ```typescript
   console.log('Job data:', job);
   console.error('API error:', error);
   ```

3. **Network tab** in browser DevTools to inspect API calls

## 📦 Building for Production

### Backend

```bash
cd server
dotnet publish -c Release -o ./publish
```

### Frontend

```bash
cd client
npm run build
# Output in dist/
```

## 🚀 Performance Tips

### Backend
- Use async/await for all I/O operations
- Add database indexes for frequently queried fields
- Implement caching for repeated AI calls
- Use pagination for large datasets

### Frontend
- Lazy load components
- Memoize expensive computations
- Debounce API calls
- Use React.memo for pure components

## 🔐 Security Considerations

1. **Never commit API keys** - use environment variables
2. **Validate all inputs** on backend
3. **Sanitize user input** before displaying
4. **Use HTTPS** in production
5. **Implement rate limiting** for API endpoints
6. **Add authentication** for production use

## 📚 Useful Resources

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [React Documentation](https://react.dev/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Tailwind CSS Docs](https://tailwindcss.com/docs)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)

## 🤝 Contributing

1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Submit a pull request

## 💡 Tips

- Keep components small and focused
- Use TypeScript types everywhere
- Write meaningful commit messages
- Comment complex logic
- Follow existing code style
- Test edge cases

---

Happy coding! 🎉

