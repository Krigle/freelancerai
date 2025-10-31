# FreelanceFinderAI

FreelanceFinderAI is a full-stack web application that leverages AI to analyze and extract structured data from freelance job postings. Users can paste job listings, and the app uses OpenRouter's API (with GPT-4o-mini) to intelligently parse key information like job title, company, skills, experience level, location, salary, and description.

## 🔒 Security First

**Important:** This project uses environment variables to protect your API keys. Before running the application, you must set up your environment:

```bash
# Quick setup (recommended)
./setup-env.sh  # macOS/Linux
# or
./setup-env.ps1  # Windows PowerShell

# Manual setup
cp .env.template .env
# Then edit .env and add your OpenRouter API key
```

**📖 See [SECURITY.md](SECURITY.md) for complete security and environment setup instructions.**

## Features

- **Job Posting Analysis**: Paste any job listing text and get AI-powered extraction of structured job data
- **Data Extraction**: Automatically identifies and extracts job title, company name, required skills, experience level, location, salary range, and description summary
- **Job Management**: View all analyzed jobs in a dashboard, delete jobs, and refresh the list with persistent storage
- **Real-time Dashboard**: Displays jobs in a responsive card-based layout with loading states and error handling
- **Secure Configuration**: Environment variable-based configuration for API keys and sensitive data
- **Responsive Design**: Mobile-friendly UI built with Tailwind CSS

## Built With

- **Backend**: C# with .NET 9 Web API, Entity Framework Core, SQLite database
- **Frontend**: React 19 with TypeScript, Vite for development, Tailwind CSS for styling, Zustand for state management
- **AI Integration**: OpenRouter API (GPT-4o-mini) for intelligent data extraction with JSON parsing and error handling
- **DevOps**: Docker and Docker Compose for containerization and local development
- **Security**: Environment variable management, .gitignore protection, secure configuration patterns
- **Other**: CORS configuration, comprehensive error handling, logging, and resilience policies (Polly)

## Learning Outcomes

This project demonstrates proficiency in:

- **Backend Development**: RESTful API design, Entity Framework Core, dependency injection, async/await patterns, error handling, and configuration management
- **Frontend Development**: React hooks, TypeScript interfaces, component composition, API integration, state management, and responsive design
- **AI Integration**: OpenAI API usage, prompt engineering, JSON parsing, and fallback strategies
- **DevOps**: Docker containerization, multi-stage builds, environment configuration, and production deployment preparation
- **Full-Stack Architecture**: Combining modern technologies with clean code practices, type safety, and comprehensive documentation
