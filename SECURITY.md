# Security & Environment Configuration

## 🔒 Protecting Your API Keys

This project uses environment variables to keep sensitive information secure. **Never commit API keys or secrets to version control!**

## Setup Instructions

### 1. Environment Variables Setup

#### For Development (Local Machine)

**Option A: Using .env file (Recommended for local development)**

1. Copy the template file:
   ```bash
   cp .env.template .env
   ```

2. Edit `.env` and add your actual API key:
   ```bash
   OPENROUTER_API_KEY=your_actual_api_key_here
   ```

3. The `.env` file is already in `.gitignore` and will **never** be committed to Git.

**Option B: Using System Environment Variables**

Set environment variables in your shell:

```bash
# macOS/Linux
export OPENROUTER_API_KEY="your_actual_api_key_here"
export OPENROUTER_BASE_URL="https://openrouter.ai/api/v1"
export OPENROUTER_MODEL="openai/gpt-4o-mini"

# Windows PowerShell
$env:OPENROUTER_API_KEY="your_actual_api_key_here"
$env:OPENROUTER_BASE_URL="https://openrouter.ai/api/v1"
$env:OPENROUTER_MODEL="openai/gpt-4o-mini"
```

#### For Production (Server/Cloud)

**Never use .env files in production!** Instead, use your hosting platform's environment variable management:

- **Azure App Service**: Application Settings
- **AWS**: Systems Manager Parameter Store or Secrets Manager
- **Heroku**: Config Vars
- **Docker**: Environment variables in docker-compose.yml or Kubernetes secrets
- **Railway/Render**: Environment Variables in dashboard

### 2. ASP.NET Core Configuration Files

The project includes:

- ✅ `appsettings.json` - Safe to commit (no secrets)
- ✅ `appsettings.Development.json.template` - Template file (safe to commit)
- ❌ `appsettings.Development.json` - **NEVER COMMIT** (contains your API key)
- ❌ `appsettings.Production.json` - **NEVER COMMIT** (if you create it)

#### Setting Up Development Configuration

1. Copy the template:
   ```bash
   cd server
   cp appsettings.Development.json.template appsettings.Development.json
   ```

2. Edit `appsettings.Development.json` and add your API key:
   ```json
   {
     "OpenAI": {
       "ApiKey": "your_actual_api_key_here"
     }
   }
   ```

**Note:** Environment variables take precedence over configuration files!

## 🔐 Environment Variables Reference

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `OPENROUTER_API_KEY` | Your OpenRouter API key | - | ✅ Yes |
| `OPENROUTER_BASE_URL` | OpenRouter API base URL | `https://openrouter.ai/api/v1` | No |
| `OPENROUTER_MODEL` | AI model to use | `openai/gpt-4o-mini` | No |
| `DATABASE_CONNECTION_STRING` | SQLite connection string | `Data Source=freelancefinder.db` | No |
| `CORS_ALLOWED_ORIGINS` | Comma-separated allowed origins | `http://localhost:3000,http://localhost:5173,http://localhost:5174` | No |
| `PORT` | Server port | `5001` | No |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development` | No |

## 🚀 Running the Application

### Development Mode

```bash
# Set environment variables (if not using .env file)
export OPENROUTER_API_KEY="your_key_here"

# Run the server
cd server
dotnet run

# Run the client (in another terminal)
cd client
npm run dev
```

### Production Mode

```bash
# Set environment variables on your server
export ASPNETCORE_ENVIRONMENT=Production
export OPENROUTER_API_KEY="your_production_key"
export DATABASE_CONNECTION_STRING="your_production_db_connection"
export CORS_ALLOWED_ORIGINS="https://yourdomain.com"

# Run the application
cd server
dotnet run --configuration Release
```

## 🛡️ Security Best Practices

### ✅ DO:
- ✅ Use environment variables for all secrets
- ✅ Keep `.env` files in `.gitignore`
- ✅ Use different API keys for development and production
- ✅ Rotate API keys regularly
- ✅ Use your hosting platform's secret management in production
- ✅ Review `.gitignore` before committing
- ✅ Use HTTPS in production
- ✅ Limit CORS origins to only trusted domains

### ❌ DON'T:
- ❌ Commit API keys to Git
- ❌ Share API keys in chat, email, or screenshots
- ❌ Use production keys in development
- ❌ Hardcode secrets in source code
- ❌ Commit `appsettings.Development.json` or `appsettings.Production.json`
- ❌ Use `AllowAnyOrigin()` in CORS for production

## 🔍 Checking for Exposed Secrets

Before committing, always check:

```bash
# Check what files will be committed
git status

# Make sure these files are NOT listed:
# - .env
# - appsettings.Development.json
# - appsettings.Production.json
# - Any file with "secret" in the name

# If you accidentally staged a secret file:
git reset HEAD <filename>
```

## 🆘 If You Accidentally Commit a Secret

1. **Immediately revoke/rotate the exposed API key** at https://openrouter.ai
2. Remove the secret from Git history:
   ```bash
   # Remove file from Git history (use with caution!)
   git filter-branch --force --index-filter \
     "git rm --cached --ignore-unmatch path/to/secret/file" \
     --prune-empty --tag-name-filter cat -- --all
   
   # Force push (only if you haven't shared the repo)
   git push origin --force --all
   ```
3. Generate a new API key
4. Update your environment variables with the new key

## 📝 Getting an OpenRouter API Key

1. Go to https://openrouter.ai
2. Sign up or log in
3. Navigate to API Keys section
4. Create a new API key
5. Copy the key and add it to your `.env` file or environment variables
6. **Never share this key publicly!**

## 🐳 Docker Environment Variables

If using Docker, pass environment variables:

```bash
# docker-compose.yml
services:
  server:
    environment:
      - OPENROUTER_API_KEY=${OPENROUTER_API_KEY}
      - ASPNETCORE_ENVIRONMENT=Production
```

Then run:
```bash
OPENROUTER_API_KEY=your_key docker-compose up
```

## 📞 Support

If you have questions about security or environment setup, please open an issue on GitHub (but **never** include your actual API keys in the issue!).

