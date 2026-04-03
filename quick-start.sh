#!/bin/bash

# ETF Investment Tracker - Quick Start Script
# This script helps set up the development environment

set -e

echo "=========================================="
echo "ETF Investment Tracker - Quick Start"
echo "=========================================="
echo ""

# Check prerequisites
echo "Checking prerequisites..."

if ! command -v psql &> /dev/null; then
    echo "❌ PostgreSQL not found. Please install PostgreSQL."
    exit 1
fi

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 10 SDK."
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "❌ Node.js not found. Please install Node.js 20+."
    exit 1
fi

if ! command -v ng &> /dev/null; then
    echo "⚠️  Angular CLI not found. Installing globally..."
    npm install -g @angular/cli
fi

echo "✅ All prerequisites found"
echo ""

# Database setup
echo "========== DATABASE SETUP =========="
read -p "PostgreSQL password: " -s db_password
echo ""

echo "Creating database..."
PGPASSWORD=$db_password psql -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'etf_tracker'" | grep -q 1 || PGPASSWORD=$db_password psql -U postgres -c "CREATE DATABASE etf_tracker;"

echo "Running schema..."
PGPASSWORD=$db_password psql -U postgres -d etf_tracker -f database_schema.sql

echo "✅ Database setup complete"
echo ""

# Backend setup
echo "========== BACKEND SETUP =========="
read -p "Eodhd API Key (leave empty to skip): " api_key
echo ""

# Update appsettings.json
cd ETFTracker.Api

if [ -n "$api_key" ]; then
    # Update API key in appsettings.json
    sed -i.bak "s/YOUR_EODHD_API_KEY_HERE/$api_key/" appsettings.json
fi

# Update connection string with password
sed -i.bak "s/Password=postgres;/Password=$db_password;/" appsettings.json

echo "Building backend..."
dotnet build

echo "✅ Backend setup complete"
echo ""

# Frontend setup
echo "========== FRONTEND SETUP =========="
cd ../ETFTracker.Web

echo "Installing dependencies..."
npm install

echo "Building frontend..."
npm run build

echo "✅ Frontend setup complete"
echo ""

echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "To run the application:"
echo ""
echo "1. Terminal 1 - Start Backend API:"
echo "   cd ETFTracker.Api"
echo "   dotnet run"
echo "   (Available at https://localhost:5000)"
echo ""
echo "2. Terminal 2 - Start Frontend Dev Server:"
echo "   cd ETFTracker.Web"
echo "   npm start"
echo "   (Available at http://localhost:4200)"
echo ""
echo "Or for production:"
echo "   Serve 'dist/ETFTracker.Web' folder with your web server"
echo ""

