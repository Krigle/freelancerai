# FreelanceFinderAI

> AI-powered job posting analyzer for freelancers. Transform messy job listings into clean, structured data.

🌐 **Live Demo**: [freelancerai.pro](https://freelancerai.pro)

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.6-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 📖 Overview

FreelanceFinderAI is a full-stack web application that uses AI to analyze freelance job postings and extract structured data. Paste any job listing from Indeed, Upwork, or any other source, and get instant AI-powered extraction of:

- 📋 Job title and company name
- 💼 Required skills and technologies
- 📊 Experience level (entry/mid/senior/lead)
- 📍 Location (remote/hybrid/on-site)
- 💰 Salary range
- 📝 Clean, formatted summary with bullet points

## ✨ Features

- **AI-Powered Extraction**: Uses OpenRouter API (GPT-4o-mini) with custom prompt engineering
- **Smart Preprocessing**: Removes webpage noise (navigation, footers, ads) before AI analysis
- **Persistent Storage**: SQLite database with Entity Framework Core
- **Real-time Dashboard**: View all analyzed jobs in a responsive card layout
- **Production-Ready**: Docker deployment, health checks, resilience policies, caching
- **Type-Safe**: TypeScript frontend, C# backend with nullable reference types
- **Modern Stack**: React 19, .NET 9, Zustand, Tailwind CSS, Vite

## 🏗️ Tech Stack

### Backend

- **Framework**: .NET 9 Web API
- **Database**: SQLite with Entity Framework Core
- **AI Integration**: OpenRouter API (GPT-4o-mini)
- **Resilience**: Polly (retry, circuit breaker, timeout)
- **Patterns**: Dependency injection, repository pattern, async/await

### Frontend

- **Framework**: React 19 with TypeScript
- **Build Tool**: Vite
- **State Management**: Zustand
- **Styling**: Tailwind CSS
- **HTTP Client**: Axios with rate limiting

```

```
