# ============================================
# FreelanceFinderAI - Production Dockerfile
# Multi-stage build for React frontend + .NET backend
# ============================================

# ============================================
# Stage 1: Build React Frontend
# ============================================
FROM node:20-alpine AS frontend-build

WORKDIR /app/client

# Copy package files
COPY client/package*.json ./

# Install dependencies
RUN npm ci --only=production

# Copy frontend source
COPY client/ ./

# Build argument for API URL (set by Coolify)
ARG VITE_API_URL=/api

# Build frontend for production
RUN npm run build

# ============================================
# Stage 2: Build .NET Backend
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build

WORKDIR /app/server

# Copy csproj and restore dependencies
COPY server/*.csproj ./
RUN dotnet restore

# Copy server source
COPY server/ ./

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# ============================================
# Stage 3: Runtime Image
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Install SQLite (needed for runtime)
RUN apt-get update && \
    apt-get install -y sqlite3 libsqlite3-dev && \
    rm -rf /var/lib/apt/lists/*

# Copy published backend
COPY --from=backend-build /app/publish ./

# Copy built frontend to wwwroot
COPY --from=frontend-build /app/client/dist ./wwwroot

# Create directory for SQLite database with proper permissions
RUN mkdir -p /app/data && \
    chmod 777 /app/data

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5001
ENV DATABASE_PATH=/app/data/freelancefinder.db

# Expose port
EXPOSE 5001

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5001/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "FreelanceFinderAI.dll"]

