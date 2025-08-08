#!/bin/bash

# SF-Server Release Build Script
# Creates a release-ready build of the server

set -e

VERSION=${1:-"1.0.0"}
OUTPUT_DIR="releases/sf-server-v${VERSION}"

echo "Building SF-Server Release v${VERSION}"
echo "======================================="

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Build the server for multiple platforms
echo "Building for Linux x64..."
dotnet publish SF-Server/SF-Server.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --output "$OUTPUT_DIR/linux-x64" \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true

echo "Building for Windows x64..."
dotnet publish SF-Server/SF-Server.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output "$OUTPUT_DIR/windows-x64" \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true

echo "Building for macOS x64..."
dotnet publish SF-Server/SF-Server.csproj \
    --configuration Release \
    --runtime osx-x64 \
    --self-contained true \
    --output "$OUTPUT_DIR/macos-x64" \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true

# Copy documentation and configuration files
echo "Copying documentation..."
cp README.md "$OUTPUT_DIR/"
cp CLIENT_SETUP.md "$OUTPUT_DIR/"
cp .env.example "$OUTPUT_DIR/"
cp docker-compose.yml "$OUTPUT_DIR/"
cp Dockerfile "$OUTPUT_DIR/"

# Create example configuration
echo "Creating example configuration..."
cat > "$OUTPUT_DIR/server_config.example.json" << EOF
{
  "Port": 1337,
  "SteamWebApiToken": "YOUR_STEAM_WEB_API_TOKEN_HERE",
  "HostSteamId": 76561198000000000,
  "MaxPlayers": 4,
  "EnableLogging": true,
  "LogPath": "debug_log.txt",
  "AuthDelayMs": 1000,
  "EnableConsoleOutput": true
}
EOF

# Create startup scripts
echo "Creating startup scripts..."

# Linux/macOS startup script
cat > "$OUTPUT_DIR/start-server.sh" << 'EOF'
#!/bin/bash
# SF-Server Startup Script

# Check for configuration
if [ ! -f "server_config.json" ]; then
    echo "Creating default configuration file..."
    cp server_config.example.json server_config.json
    echo "Please edit server_config.json with your Steam Web API token and Steam ID"
    echo "You can get a Steam Web API key at: https://steamcommunity.com/dev/apikey"
    exit 1
fi

# Determine platform
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORM="linux-x64"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="macos-x64"
else
    echo "Unsupported platform: $OSTYPE"
    exit 1
fi

# Run the server
echo "Starting SF-Server..."
./${PLATFORM}/SF-Server server_config.json
EOF

# Windows startup script
cat > "$OUTPUT_DIR/start-server.bat" << 'EOF'
@echo off
REM SF-Server Startup Script for Windows

REM Check for configuration
if not exist "server_config.json" (
    echo Creating default configuration file...
    copy server_config.example.json server_config.json
    echo Please edit server_config.json with your Steam Web API token and Steam ID
    echo You can get a Steam Web API key at: https://steamcommunity.com/dev/apikey
    pause
    exit /b 1
)

REM Run the server
echo Starting SF-Server...
windows-x64\SF-Server.exe server_config.json
pause
EOF

# Make scripts executable
chmod +x "$OUTPUT_DIR/start-server.sh"
chmod +x "$OUTPUT_DIR/linux-x64/SF-Server"
chmod +x "$OUTPUT_DIR/macos-x64/SF-Server"

# Create archive
echo "Creating release archive..."
cd releases
tar -czf "sf-server-v${VERSION}.tar.gz" "sf-server-v${VERSION}"
zip -r "sf-server-v${VERSION}.zip" "sf-server-v${VERSION}"
cd ..

echo ""
echo "✅ Release build completed successfully!"
echo ""
echo "Release files created:"
echo "  - $OUTPUT_DIR/ (extracted files)"
echo "  - releases/sf-server-v${VERSION}.tar.gz (Linux/macOS)"
echo "  - releases/sf-server-v${VERSION}.zip (Windows)"
echo ""
echo "Platform binaries:"
echo "  - Linux x64: $OUTPUT_DIR/linux-x64/SF-Server"
echo "  - Windows x64: $OUTPUT_DIR/windows-x64/SF-Server.exe"
echo "  - macOS x64: $OUTPUT_DIR/macos-x64/SF-Server"
echo ""
echo "To use:"
echo "  1. Extract the archive for your platform"
echo "  2. Copy server_config.example.json to server_config.json"
echo "  3. Edit server_config.json with your Steam credentials"
echo "  4. Run start-server.sh (Linux/macOS) or start-server.bat (Windows)"