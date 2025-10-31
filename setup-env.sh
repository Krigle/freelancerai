#!/bin/bash

# FreelanceFinderAI Environment Setup Script
# This script helps you set up your environment variables securely

set -e

echo "🔒 FreelanceFinderAI - Secure Environment Setup"
echo "================================================"
echo ""

# Check if .env already exists
if [ -f ".env" ]; then
    echo "⚠️  .env file already exists!"
    read -p "Do you want to overwrite it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "❌ Setup cancelled. Your existing .env file was not modified."
        exit 0
    fi
fi

# Check if template exists
if [ ! -f ".env.template" ]; then
    echo "❌ Error: .env.template not found!"
    exit 1
fi

# Copy template
cp .env.template .env
echo "✅ Created .env file from template"
echo ""

# Prompt for API key
echo "📝 Please enter your OpenRouter API key:"
echo "   (Get one at: https://openrouter.ai)"
read -p "API Key: " -r API_KEY

if [ -z "$API_KEY" ]; then
    echo "❌ Error: API key cannot be empty!"
    rm .env
    exit 1
fi

# Update .env file with the API key
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s|OPENROUTER_API_KEY=.*|OPENROUTER_API_KEY=$API_KEY|" .env
else
    # Linux
    sed -i "s|OPENROUTER_API_KEY=.*|OPENROUTER_API_KEY=$API_KEY|" .env
fi

echo "✅ API key configured in .env file"
echo ""

# Set up server configuration
echo "📝 Setting up server configuration..."

if [ ! -f "server/appsettings.Development.json" ]; then
    if [ -f "server/appsettings.Development.json.template" ]; then
        cp server/appsettings.Development.json.template server/appsettings.Development.json
        
        # Update the API key in appsettings.Development.json
        if [[ "$OSTYPE" == "darwin"* ]]; then
            # macOS
            sed -i '' "s|YOUR_OPENROUTER_API_KEY_HERE|$API_KEY|" server/appsettings.Development.json
        else
            # Linux
            sed -i "s|YOUR_OPENROUTER_API_KEY_HERE|$API_KEY|" server/appsettings.Development.json
        fi
        
        echo "✅ Created server/appsettings.Development.json"
    else
        echo "⚠️  Warning: server/appsettings.Development.json.template not found"
    fi
else
    echo "ℹ️  server/appsettings.Development.json already exists (not modified)"
fi

echo ""
echo "✅ Environment setup complete!"
echo ""
echo "🔐 Security Reminders:"
echo "   • Never commit .env or appsettings.Development.json to Git"
echo "   • These files are already in .gitignore"
echo "   • Keep your API key secret and secure"
echo ""
echo "🚀 Next steps:"
echo "   1. Start the server: cd server && dotnet run"
echo "   2. Start the client: cd client && npm run dev"
echo "   3. Open http://localhost:5174 in your browser"
echo ""
echo "📖 For more information, see SECURITY.md"

