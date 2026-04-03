#!/bin/bash

# ETF Investment Tracker - Render.com Deployment Quick Start
# This script helps you push your code and provides the next steps

echo "=========================================="
echo "🚀 Render.com Deployment Helper"
echo "=========================================="
echo ""

# Check if git is initialized
if [ ! -d .git ]; then
    echo "❌ Git repository not found!"
    echo "Please initialize git first:"
    echo "  git init"
    echo "  git add ."
    echo "  git commit -m 'Initial commit'"
    echo "  git remote add origin https://github.com/YOUR_USERNAME/bob-esponja.git"
    echo "  git branch -M main"
    exit 1
fi

echo "📤 Pushing deployment files to GitHub..."
echo ""

# Add the new deployment files
git add Dockerfile .dockerignore RENDER_DEPLOYMENT_STEPS.md ETFTracker.Api/appsettings.Production.json ETFTracker.Api/Program.cs

# Check if there are changes to commit
if git diff --cached --quiet; then
    echo "⚠️  No changes to commit. Files might already be up to date."
    exit 0
fi

# Commit the files
git commit -m "Add Docker configuration for Render.com deployment"

if [ $? -eq 0 ]; then
    echo "✅ Changes committed successfully"
    echo ""
    echo "📡 Pushing to GitHub (main branch)..."
    git push origin main
    
    if [ $? -eq 0 ]; then
        echo "✅ Files pushed successfully!"
        echo ""
        echo "=========================================="
        echo "✨ Next Steps:"
        echo "=========================================="
        echo ""
        echo "1️⃣  Read the deployment guide:"
        echo "   Open: RENDER_DEPLOYMENT_STEPS.md"
        echo ""
        echo "2️⃣  Create PostgreSQL Database on Render:"
        echo "   • Go to https://dashboard.render.com"
        echo "   • Click 'New +' → PostgreSQL"
        echo "   • Save your database credentials"
        echo ""
        echo "3️⃣  Create Web Service on Render:"
        echo "   • Click 'New +' → Web Service"
        echo "   • Connect your GitHub repo"
        echo "   • Set Runtime to 'Docker'"
        echo "   • Add environment variables (see guide)"
        echo ""
        echo "4️⃣  Monitor Deployment:"
        echo "   • Watch logs in Render Dashboard"
        echo "   • Wait for 'Application startup complete'"
        echo ""
        echo "5️⃣  Test Your App:"
        echo "   • Visit: https://etf-tracker-app.onrender.com"
        echo "   • Test API: https://etf-tracker-app.onrender.com/api/holdings"
        echo ""
        echo "=========================================="
        echo "📖 For detailed instructions, see:"
        echo "   RENDER_DEPLOYMENT_STEPS.md"
        echo "=========================================="
        echo ""
    else
        echo "❌ Failed to push to GitHub"
        echo "Check your connection and try again"
        exit 1
    fi
else
    echo "❌ Failed to commit changes"
    exit 1
fi

