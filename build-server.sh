#!/bin/bash

# SF-Server Build Script
# This script builds the SF-Server project

set -e

echo "Building SF-Server..."
echo "====================="

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0 SDK or later"
    exit 1
fi

# Show .NET version
echo "Using .NET version: $(dotnet --version)"

# Change to project directory
cd "$(dirname "$0")/SF-Server"

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build the project
echo "Building project..."
dotnet build --configuration Release --no-restore

# Check if build was successful
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Build completed successfully!"
    echo "Server executable is located at: bin/Release/net8.0/SF-Server.dll"
    echo ""
    echo "To run the server:"
    echo "  dotnet run --configuration Release -- --steam_web_api_token YOUR_TOKEN --host_steamid YOUR_STEAMID"
    echo ""
    echo "Or run the built executable:"
    echo "  dotnet bin/Release/net8.0/SF-Server.dll --steam_web_api_token YOUR_TOKEN --host_steamid YOUR_STEAMID"
else
    echo "❌ Build failed!"
    exit 1
fi