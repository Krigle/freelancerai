# FreelanceFinderAI Environment Setup Script (PowerShell)
# This script helps you set up your environment variables securely

Write-Host "🔒 FreelanceFinderAI - Secure Environment Setup" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if .env already exists
if (Test-Path ".env") {
    Write-Host "⚠️  .env file already exists!" -ForegroundColor Yellow
    $overwrite = Read-Host "Do you want to overwrite it? (y/N)"
    if ($overwrite -ne "y" -and $overwrite -ne "Y") {
        Write-Host "❌ Setup cancelled. Your existing .env file was not modified." -ForegroundColor Red
        exit 0
    }
}

# Check if template exists
if (-not (Test-Path ".env.template")) {
    Write-Host "❌ Error: .env.template not found!" -ForegroundColor Red
    exit 1
}

# Copy template
Copy-Item ".env.template" ".env"
Write-Host "✅ Created .env file from template" -ForegroundColor Green
Write-Host ""

# Prompt for API key
Write-Host "📝 Please enter your OpenRouter API key:" -ForegroundColor Cyan
Write-Host "   (Get one at: https://openrouter.ai)" -ForegroundColor Gray
$apiKey = Read-Host "API Key"

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "❌ Error: API key cannot be empty!" -ForegroundColor Red
    Remove-Item ".env"
    exit 1
}

# Update .env file with the API key
$envContent = Get-Content ".env"
$envContent = $envContent -replace "OPENROUTER_API_KEY=.*", "OPENROUTER_API_KEY=$apiKey"
$envContent | Set-Content ".env"

Write-Host "✅ API key configured in .env file" -ForegroundColor Green
Write-Host ""

# Set up server configuration
Write-Host "📝 Setting up server configuration..." -ForegroundColor Cyan

if (-not (Test-Path "server/appsettings.Development.json")) {
    if (Test-Path "server/appsettings.Development.json.template") {
        Copy-Item "server/appsettings.Development.json.template" "server/appsettings.Development.json"
        
        # Update the API key in appsettings.Development.json
        $appSettingsContent = Get-Content "server/appsettings.Development.json"
        $appSettingsContent = $appSettingsContent -replace "YOUR_OPENROUTER_API_KEY_HERE", $apiKey
        $appSettingsContent | Set-Content "server/appsettings.Development.json"
        
        Write-Host "✅ Created server/appsettings.Development.json" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Warning: server/appsettings.Development.json.template not found" -ForegroundColor Yellow
    }
} else {
    Write-Host "ℹ️  server/appsettings.Development.json already exists (not modified)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ Environment setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "🔐 Security Reminders:" -ForegroundColor Yellow
Write-Host "   • Never commit .env or appsettings.Development.json to Git"
Write-Host "   • These files are already in .gitignore"
Write-Host "   • Keep your API key secret and secure"
Write-Host ""
Write-Host "🚀 Next steps:" -ForegroundColor Cyan
Write-Host "   1. Start the server: cd server; dotnet run"
Write-Host "   2. Start the client: cd client; npm run dev"
Write-Host "   3. Open http://localhost:5174 in your browser"
Write-Host ""
Write-Host "📖 For more information, see SECURITY.md" -ForegroundColor Gray

