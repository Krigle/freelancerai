# Axios Quick Reference Guide

## Import the API Client

```typescript
import { api, handleApiError } from './axios.config';
```

## Basic Requests

### GET Request
```typescript
// Simple GET
const response = await api.get<Job[]>('/jobs');
const jobs = response.data;

// GET with path parameters
const response = await api.get<Job>(`/jobs/${id}`);
const job = response.data;

// GET with query parameters
const response = await api.get<Job[]>('/jobs', {
  params: {
    limit: 10,
    offset: 0,
    status: 'active'
  }
});
```

### POST Request
```typescript
// POST with JSON body
const response = await api.post<Job>('/jobs/analyze', {
  jobText: 'Job description here...'
});
const job = response.data;

// POST with custom headers
const response = await api.post<Job>('/jobs', data, {
  headers: {
    'X-Custom-Header': 'value'
  }
});
```

### PUT Request
```typescript
// Update a resource
const response = await api.put<Job>(`/jobs/${id}`, {
  title: 'Updated Title',
  company: 'Updated Company'
});
```

### PATCH Request
```typescript
// Partial update
const response = await api.patch<Job>(`/jobs/${id}`, {
  title: 'New Title'
});
```

### DELETE Request
```typescript
// Delete a resource
await api.delete(`/jobs/${id}`);
```

## Error Handling

### Try-Catch Pattern
```typescript
try {
  const response = await api.get<Job[]>('/jobs');
  return response.data;
} catch (error) {
  return handleApiError(error);
}
```

### Check Error Type
```typescript
import axios from 'axios';

try {
  const response = await api.get('/jobs');
} catch (error) {
  if (axios.isAxiosError(error)) {
    // It's an Axios error
    console.log(error.response?.status);
    console.log(error.response?.data);
  } else {
    // It's a different error
    console.error(error);
  }
}
```

### Handle Specific Status Codes
```typescript
try {
  const response = await api.get('/jobs');
} catch (error) {
  if (axios.isAxiosError(error)) {
    switch (error.response?.status) {
      case 404:
        console.log('Not found');
        break;
      case 401:
        console.log('Unauthorized');
        break;
      case 500:
        console.log('Server error');
        break;
    }
  }
}
```

## Advanced Features

### Request Cancellation
```typescript
const controller = new AbortController();

try {
  const response = await api.get('/jobs', {
    signal: controller.signal
  });
} catch (error) {
  if (axios.isCancel(error)) {
    console.log('Request cancelled');
  }
}

// Cancel the request
controller.abort();
```

### Timeout
```typescript
// Override default timeout (30s) for a specific request
const response = await api.get('/jobs', {
  timeout: 5000 // 5 seconds
});
```

### Custom Headers
```typescript
const response = await api.get('/jobs', {
  headers: {
    'Authorization': 'Bearer token',
    'X-Custom-Header': 'value'
  }
});
```

### Response Type
```typescript
// Download a file
const response = await api.get('/export', {
  responseType: 'blob'
});

// Get raw text
const response = await api.get('/data', {
  responseType: 'text'
});
```

### Upload Progress
```typescript
const formData = new FormData();
formData.append('file', file);

const response = await api.post('/upload', formData, {
  headers: {
    'Content-Type': 'multipart/form-data'
  },
  onUploadProgress: (progressEvent) => {
    const percentCompleted = Math.round(
      (progressEvent.loaded * 100) / (progressEvent.total || 1)
    );
    console.log(`Upload: ${percentCompleted}%`);
  }
});
```

### Download Progress
```typescript
const response = await api.get('/download', {
  responseType: 'blob',
  onDownloadProgress: (progressEvent) => {
    const percentCompleted = Math.round(
      (progressEvent.loaded * 100) / (progressEvent.total || 1)
    );
    console.log(`Download: ${percentCompleted}%`);
  }
});
```

## Configuration

### Create Custom Instance
```typescript
import axios from 'axios';

const customApi = axios.create({
  baseURL: 'https://api.example.com',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
});
```

### Add Request Interceptor
```typescript
api.interceptors.request.use(
  (config) => {
    // Modify request before sending
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);
```

### Add Response Interceptor
```typescript
api.interceptors.response.use(
  (response) => {
    // Transform response data
    return response;
  },
  (error) => {
    // Handle errors globally
    if (error.response?.status === 401) {
      // Redirect to login
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

## TypeScript Tips

### Type the Response
```typescript
interface Job {
  id: number;
  title: string;
}

// Type the response data
const response = await api.get<Job[]>('/jobs');
const jobs: Job[] = response.data; // Typed!
```

### Type the Error
```typescript
import { AxiosError } from 'axios';

interface ErrorResponse {
  error: string;
  code: string;
}

try {
  await api.get('/jobs');
} catch (error) {
  const axiosError = error as AxiosError<ErrorResponse>;
  console.log(axiosError.response?.data.error);
}
```

### Create Typed API Functions
```typescript
async function getJobs(): Promise<Job[]> {
  const response = await api.get<Job[]>('/jobs');
  return response.data;
}

async function getJob(id: number): Promise<Job> {
  const response = await api.get<Job>(`/jobs/${id}`);
  return response.data;
}
```

## Common Patterns

### Fetch on Component Mount
```typescript
import { useEffect, useState } from 'react';

function JobList() {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchJobs = async () => {
      try {
        const response = await api.get<Job[]>('/jobs');
        setJobs(response.data);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    };

    fetchJobs();
  }, []);

  if (loading) return <div>Loading...</div>;
  return <div>{jobs.map(job => ...)}</div>;
}
```

### Form Submission
```typescript
async function handleSubmit(data: FormData) {
  try {
    const response = await api.post<Job>('/jobs', data);
    console.log('Success:', response.data);
  } catch (error) {
    if (axios.isAxiosError(error)) {
      console.error('Error:', error.response?.data);
    }
  }
}
```

### Parallel Requests
```typescript
// Execute multiple requests in parallel
const [jobs, users, settings] = await Promise.all([
  api.get<Job[]>('/jobs'),
  api.get<User[]>('/users'),
  api.get<Settings>('/settings')
]);

console.log(jobs.data, users.data, settings.data);
```

### Sequential Requests
```typescript
// Execute requests one after another
const jobResponse = await api.get<Job>(`/jobs/${id}`);
const job = jobResponse.data;

const detailsResponse = await api.get<Details>(`/jobs/${id}/details`);
const details = detailsResponse.data;
```

## Debugging

### Log All Requests
```typescript
api.interceptors.request.use((config) => {
  console.log('Request:', config.method, config.url, config.data);
  return config;
});
```

### Log All Responses
```typescript
api.interceptors.response.use((response) => {
  console.log('Response:', response.status, response.data);
  return response;
});
```

### Inspect Request/Response
```typescript
const response = await api.get('/jobs');

console.log('Status:', response.status);
console.log('Headers:', response.headers);
console.log('Data:', response.data);
console.log('Config:', response.config);
```

## Cheat Sheet

```typescript
// Import
import { api } from './axios.config';

// GET
const { data } = await api.get<T>('/endpoint');

// POST
const { data } = await api.post<T>('/endpoint', body);

// PUT
const { data } = await api.put<T>('/endpoint', body);

// PATCH
const { data } = await api.patch<T>('/endpoint', body);

// DELETE
await api.delete('/endpoint');

// With params
await api.get('/endpoint', { params: { key: 'value' } });

// With headers
await api.get('/endpoint', { headers: { 'Key': 'Value' } });

// Error handling
try {
  const { data } = await api.get('/endpoint');
} catch (error) {
  if (axios.isAxiosError(error)) {
    console.log(error.response?.data);
  }
}
```

