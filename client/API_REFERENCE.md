# API Reference

## Base URL

```
http://localhost:5001/api
```

Configured in `client/.env`:
```
VITE_API_URL=http://localhost:5001/api
```

---

## Endpoints

### 1. Get All Jobs

**Endpoint:** `GET /jobs`

**Description:** Retrieve all analyzed jobs, ordered by creation date (newest first)

**Request:**
```typescript
const jobs = await getJobs();
```

**Response:**
```typescript
Job[] // Array of Job objects
```

**Example Response:**
```json
[
  {
    "id": 6,
    "originalText": "Full job posting text...",
    "extractedJson": "{\"Title\":\"Junior Full Stack Developer\",...}",
    "extracted": {
      "title": "Junior Full Stack Developer",
      "company": "Shoutt International Ltd",
      "skills": ["C#", "React", "Back-end development"],
      "experienceLevel": "entry",
      "location": "Remote",
      "salaryRange": "£40,000 a year",
      "descriptionSummary": "We're looking for a Full Stack Developer..."
    },
    "createdAt": "2025-10-30T09:08:58.254289"
  }
]
```

**Zustand Store:**
```typescript
const { fetchJobs, jobs, loading, error } = useJobStore();

// Fetch jobs
await fetchJobs();

// Access jobs
console.log(jobs);
```

---

### 2. Analyze Job

**Endpoint:** `POST /jobs/analyze`

**Description:** Analyze a job posting using AI and save it to the database

**Request:**
```typescript
const job = await analyzeJob(jobText);
```

**Request Body:**
```typescript
{
  jobText: string; // The full job posting text
}
```

**Response:**
```typescript
Job // Single Job object with extracted data
```

**Example Request:**
```json
{
  "jobText": "We're hiring a Senior React Developer..."
}
```

**Example Response:**
```json
{
  "id": 7,
  "originalText": "We're hiring a Senior React Developer...",
  "extractedJson": "{\"Title\":\"Senior React Developer\",...}",
  "extracted": {
    "title": "Senior React Developer",
    "company": "Tech Corp",
    "skills": ["React", "TypeScript", "Node.js"],
    "experienceLevel": "senior",
    "location": "San Francisco, CA",
    "salaryRange": "$120,000 - $150,000",
    "descriptionSummary": "Looking for an experienced React developer..."
  },
  "createdAt": "2025-10-30T10:15:30.123456"
}
```

**Zustand Store:**
```typescript
const { analyzeJob, analyzing, analyzeError } = useJobStore();

// Analyze job (with optimistic update)
const job = await analyzeJob("Job posting text...");

if (job) {
  console.log("Job analyzed:", job);
} else {
  console.error("Analysis failed:", analyzeError);
}
```

**Optimistic Update:**
- Job appears immediately in UI with temporary ID
- Replaced with real job when API responds
- Removed if API call fails

---

### 3. Get Single Job

**Endpoint:** `GET /jobs/{id}`

**Description:** Retrieve a specific job by ID

**Request:**
```typescript
const job = await getJob(id);
```

**Parameters:**
- `id` (number) - Job ID

**Response:**
```typescript
Job // Single Job object
```

**Example Response:**
```json
{
  "id": 6,
  "originalText": "...",
  "extractedJson": "...",
  "extracted": { ... },
  "createdAt": "2025-10-30T09:08:58.254289"
}
```

**Error Response (404):**
```json
{
  "error": "Job not found"
}
```

**Note:** This endpoint is available but not currently used in the Zustand store.

---

### 4. Delete Job

**Endpoint:** `DELETE /jobs/{id}`

**Description:** Delete a specific job by ID

**Request:**
```typescript
await deleteJob(id);
```

**Parameters:**
- `id` (number) - Job ID

**Response:**
- `204 No Content` on success

**Error Response (404):**
```json
{
  "error": "Job not found"
}
```

**Zustand Store:**
```typescript
const { deleteJob } = useJobStore();

// Delete job (with optimistic update)
await deleteJob(6);
```

**Optimistic Update:**
- Job removed immediately from UI
- Restored if API call fails

---

## Type Definitions

### Job

```typescript
interface Job {
  id: number;
  originalText: string;
  extractedJson: string;
  extracted: ExtractedJobData | null;
  createdAt: string; // ISO 8601 format
}
```

### ExtractedJobData

```typescript
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

### JobRequestDto

```typescript
interface JobRequestDto {
  jobText: string;
}
```

---

## Error Handling

### Error Response Format

All errors return a JSON object with an `error` field:

```json
{
  "error": "Error message here"
}
```

### HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | Success | Job retrieved successfully |
| 204 | No Content | Job deleted successfully |
| 400 | Bad Request | Job text cannot be empty |
| 404 | Not Found | Job not found |
| 500 | Server Error | Failed to analyze job posting |

### Client Error Handling

The Axios error handler automatically converts server errors to user-friendly messages:

```typescript
try {
  const job = await analyzeJob(jobText);
} catch (error) {
  // Error is automatically formatted by handleApiError
  console.error(error.message);
  // Examples:
  // "Bad request"
  // "Resource not found"
  // "Server error - Please try again later"
}
```

---

## Usage Examples

### Fetch and Display Jobs

```typescript
import { useJobStore } from './store/useJobStore';

function JobList() {
  const { jobs, loading, error, fetchJobs } = useJobStore();
  
  useEffect(() => {
    fetchJobs();
  }, [fetchJobs]);
  
  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  
  return (
    <div>
      {jobs.map(job => (
        <div key={job.id}>
          <h3>{job.extracted?.title}</h3>
          <p>{job.extracted?.company}</p>
        </div>
      ))}
    </div>
  );
}
```

### Analyze a Job

```typescript
import { useJobStore } from './store/useJobStore';

function JobForm() {
  const { analyzeJob, analyzing, analyzeError } = useJobStore();
  const [jobText, setJobText] = useState('');
  
  const handleSubmit = async (e) => {
    e.preventDefault();
    const job = await analyzeJob(jobText);
    
    if (job) {
      setJobText(''); // Clear form
      alert('Job analyzed successfully!');
    }
  };
  
  return (
    <form onSubmit={handleSubmit}>
      <textarea
        value={jobText}
        onChange={(e) => setJobText(e.target.value)}
        disabled={analyzing}
      />
      <button type="submit" disabled={analyzing}>
        {analyzing ? 'Analyzing...' : 'Analyze Job'}
      </button>
      {analyzeError && <div>Error: {analyzeError}</div>}
    </form>
  );
}
```

### Delete a Job

```typescript
import { useJobStore } from './store/useJobStore';

function JobCard({ job }) {
  const { deleteJob } = useJobStore();
  
  const handleDelete = async () => {
    if (confirm('Delete this job?')) {
      await deleteJob(job.id);
    }
  };
  
  return (
    <div>
      <h3>{job.extracted?.title}</h3>
      <button onClick={handleDelete}>Delete</button>
    </div>
  );
}
```

### Filter and Sort Jobs

```typescript
import { useJobStore } from './store/useJobStore';

function Dashboard() {
  const {
    getPaginatedJobs,
    setFilters,
    setSortField,
    toggleSortOrder,
  } = useJobStore();
  
  const jobs = getPaginatedJobs();
  
  return (
    <div>
      <input
        type="text"
        onChange={(e) => setFilters({ searchQuery: e.target.value })}
        placeholder="Search jobs..."
      />
      
      <button onClick={() => setSortField('title')}>
        Sort by Title
      </button>
      
      <button onClick={toggleSortOrder}>
        Toggle Order
      </button>
      
      {jobs.map(job => (
        <div key={job.id}>{job.extracted?.title}</div>
      ))}
    </div>
  );
}
```

---

## Configuration

### Axios Configuration

**File:** `client/src/api/axios.config.ts`

**Settings:**
- Base URL: `http://localhost:5001/api`
- Timeout: 30 seconds
- Content-Type: `application/json`

**Interceptors:**
- Request logging (development only)
- Response logging (development only)
- Error handling with user-friendly messages

### Environment Variables

**File:** `client/.env`

```env
VITE_API_URL=http://localhost:5001/api
```

To change the API URL:
1. Update `VITE_API_URL` in `.env`
2. Restart the dev server

---

## Testing

### Manual API Testing

```bash
# Get all jobs
curl http://localhost:5001/api/jobs

# Get single job
curl http://localhost:5001/api/jobs/6

# Analyze job
curl -X POST http://localhost:5001/api/jobs/analyze \
  -H "Content-Type: application/json" \
  -d '{"jobText":"We are hiring a developer..."}'

# Delete job
curl -X DELETE http://localhost:5001/api/jobs/6
```

### Browser Console Testing

```javascript
// Import the API functions
import { getJobs, analyzeJob, deleteJob } from './api/jobs';

// Test get jobs
const jobs = await getJobs();
console.log(jobs);

// Test analyze job
const job = await analyzeJob("Test job posting");
console.log(job);

// Test delete job
await deleteJob(6);
```

---

## Troubleshooting

### API Not Responding

**Problem:** Network error or timeout

**Solutions:**
1. Check server is running: `lsof -i :5001`
2. Check `.env` has correct URL
3. Restart dev server: `npm run dev`
4. Check CORS settings in server

### Type Errors

**Problem:** TypeScript errors with API responses

**Solutions:**
1. Verify types match server models
2. Check `client/src/types/index.ts`
3. Run `npm run build` to check for errors

### Optimistic Updates Not Working

**Problem:** UI doesn't update immediately

**Solutions:**
1. Check Zustand store implementation
2. Verify component uses store correctly
3. Check browser console for errors

---

## Best Practices

1. **Always use the Zustand store** instead of calling API functions directly
2. **Handle loading states** to show feedback to users
3. **Handle errors** and display user-friendly messages
4. **Use optimistic updates** for better UX
5. **Type everything** with TypeScript for safety
6. **Test API calls** before deploying

---

## Summary

✅ **4 API endpoints** - All working correctly
✅ **Type-safe** - Full TypeScript coverage
✅ **Error handling** - User-friendly error messages
✅ **Optimistic updates** - Instant UI feedback
✅ **Zustand integration** - Centralized state management
✅ **Well documented** - Clear examples and usage

The API is production-ready and fully integrated with the Zustand store! 🚀

