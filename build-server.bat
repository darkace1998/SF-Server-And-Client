@echo off
REM SF-Server Build Script for Windows
REM This script builds the SF-Server project

echo Building SF-Server...
echo =====================

REM Check if .NET is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK or later
    pause
    exit /b 1
)

REM Show .NET version
echo Using .NET version:
dotnet --version

REM Change to project directory
cd /d "%~dp0SF-Server"

REM Restore dependencies
echo Restoring dependencies...
dotnet restore
if errorlevel 1 (
    echo Failed to restore dependencies
    pause
    exit /b 1
)

REM Build the project
echo Building project...
dotnet build --configuration Release --no-restore
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo ✅ Build completed successfully!
echo Server executable is located at: bin\Release\net8.0\SF-Server.dll
echo.
echo To run the server:
echo   dotnet run --configuration Release -- --steam_web_api_token YOUR_TOKEN --host_steamid YOUR_STEAMID
echo.
echo Or run the built executable:
echo   dotnet bin\Release\net8.0\SF-Server.dll --steam_web_api_token YOUR_TOKEN --host_steamid YOUR_STEAMID
echo.
pause