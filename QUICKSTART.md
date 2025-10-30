# 🚀 Quick Start Guide

Get FreelanceFinderAI up and running in 5 minutes!

## Prerequisites Check

Before starting, make sure you have:
- ✅ .NET 8 SDK installed (`dotnet --version`)
- ✅ Node.js 20+ installed (`node --version`)
- ✅ (Optional) OpenAI API key

## Step 1: Install Dependencies

### Backend
```bash
cd server
dotnet restore
```

### Frontend
```bash
cd client
npm install
```

## Step 2: Configure (Optional)

If you want to use real AI extraction instead of mock data:

1. Get an OpenAI API key from https://platform.openai.com/api-keys
2. Edit `server/appsettings.Development.json`:
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-your-key-here"
     }
   }
   ```

## Step 3: Run the Application

### Terminal 1 - Start Backend
```bash
cd server
dotnet run
```

You should see:
```
Now listening on: http://localhost:5000
```

### Terminal 2 - Start Frontend
```bash
cd client
npm run dev
```

You should see:
```
Local: http://localhost:5173/
```

## Step 4: Use the App

1. Open http://localhost:5173 in your browser
2. Paste a job listing (or use the sample below)
3. Click "Analyze Job Posting"
4. View the extracted data!

### Sample Job Posting to Test

```
Senior React Developer - TechStartup Inc.

We're seeking a talented Senior React Developer to join our growing team!

Location: Remote (US/Canada)
Salary: $130,000 - $170,000

Requirements:
- 5+ years of React experience
- TypeScript expertise
- Experience with Next.js
- Strong CSS skills (Tailwind preferred)
- GraphQL knowledge

Experience Level: Senior
Type: Full-time
```

## Troubleshooting

### Backend won't start
- Make sure port 5000 is not in use
- Check that .NET 8 SDK is installed: `dotnet --version`

### Frontend won't start
- Make sure port 5173 is not in use
- Delete `node_modules` and run `npm install` again
- Check Node version: `node --version` (should be 20+)

### CORS errors
- Make sure the backend is running on port 5000
- Check that `client/.env` has `VITE_API_URL=http://localhost:5000/api`

### AI extraction returns mock data
- This is normal if you haven't configured an OpenAI API key
- The app will still work, just with placeholder data
- To use real AI, add your OpenAI API key to `appsettings.Development.json`

## Next Steps

- ✨ Try analyzing different job postings
- 🎨 Customize the UI in `client/src/components/`
- 🔧 Modify the AI prompt in `server/Services/AiExtractionService.cs`
- 🚀 Deploy to production (see README.md)

## Need Help?

Check the full [README.md](README.md) for:
- Detailed architecture
- API documentation
- Deployment guides
- Docker setup

---

Happy coding! 🎉

