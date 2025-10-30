# FreelanceFinderAI

FreelanceFinderAI is a full-stack web application that leverages AI to analyze and extract structured data from freelance job postings. Users can paste job listings, and the app uses OpenAI's API to intelligently parse key information like job title, company, skills, experience level, location, salary, and description.

## Features

- **Job Posting Analysis**: Paste any job listing text and get AI-powered extraction of structured job data
- **Data Extraction**: Automatically identifies and extracts job title, company name, required skills, experience level, location, salary range, and description summary
- **Job Management**: View all analyzed jobs in a dashboard, delete jobs, and refresh the list with persistent storage
- **Real-time Dashboard**: Displays jobs in a responsive card-based layout with loading states and error handling
- **Mock Mode**: Fallback functionality when OpenAI API key is not configured
- **Responsive Design**: Mobile-friendly UI built with Tailwind CSS

## Built With

- **Backend**: C# with .NET 9 Web API, Entity Framework Core, SQLite database
- **Frontend**: React 18 with TypeScript, Vite for development, Tailwind CSS for styling
- **AI Integration**: OpenAI API for intelligent data extraction with JSON parsing and error handling
- **DevOps**: Docker and Docker Compose for containerization and local development
- **Other**: CORS configuration, comprehensive error handling, logging, and environment variable management

## Learning Outcomes

This project demonstrates proficiency in:

- **Backend Development**: RESTful API design, Entity Framework Core, dependency injection, async/await patterns, error handling, and configuration management
- **Frontend Development**: React hooks, TypeScript interfaces, component composition, API integration, state management, and responsive design
- **AI Integration**: OpenAI API usage, prompt engineering, JSON parsing, and fallback strategies
- **DevOps**: Docker containerization, multi-stage builds, environment configuration, and production deployment preparation
- **Full-Stack Architecture**: Combining modern technologies with clean code practices, type safety, and comprehensive documentation
