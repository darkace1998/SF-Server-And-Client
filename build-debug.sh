#!/bin/bash

# SF-Server and Client Build and Debug Script
# Usage: ./build-debug.sh [server|client|all]

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$PROJECT_ROOT/SF-Server"
CLIENT_DIR="$PROJECT_ROOT/SF_Lidgren"

build_server() {
    echo "=== Building SF-Server ==="
    cd "$SERVER_DIR"
    dotnet restore
    dotnet build
    echo "✅ Server build complete"
    echo "📍 Server executable: $SERVER_DIR/bin/Debug/net8.0/SF-Server.dll"
}

build_client() {
    echo "=== Building SF_Lidgren Client ==="
    cd "$CLIENT_DIR"
    dotnet restore  
    dotnet build
    echo "✅ Client build complete"
    echo "📍 Client plugin: $CLIENT_DIR/bin/Debug/net35/SF_Lidgren.dll"
    echo "💡 Copy this DLL to your game's BepInEx/plugins/ directory"
}

run_server_debug() {
    echo "=== Starting Server in Debug Mode ==="
    echo "⚠️  Using dummy credentials - replace with real tokens for production"
    cd "$SERVER_DIR"
    dotnet run -- --steam_web_api_token DUMMY_TOKEN --host_steamid 76561198000000000
}

show_usage() {
    echo "Usage: $0 [server|client|all|run-server-debug]"
    echo ""
    echo "Commands:"
    echo "  server           Build only the server"
    echo "  client           Build only the client" 
    echo "  all              Build both server and client (default)"
    echo "  run-server-debug Start server with dummy credentials for debugging"
    echo ""
    echo "Examples:"
    echo "  $0               # Build everything"
    echo "  $0 client        # Build only client"
    echo "  $0 run-server-debug  # Start server for debugging"
}

case "${1:-all}" in
    "server")
        build_server
        ;;
    "client")
        build_client
        ;;
    "all")
        build_server
        build_client
        echo ""
        echo "🎉 Build complete! Both server and client are ready."
        echo ""
        echo "Next steps:"
        echo "1. For server: Use 'run-server-debug' or set real Steam credentials"
        echo "2. For client: Copy SF_Lidgren.dll to your game's BepInEx/plugins/"
        ;;
    "run-server-debug")
        run_server_debug
        ;;
    "-h"|"--help"|"help")
        show_usage
        ;;
    *)
        echo "❌ Unknown command: $1"
        show_usage
        exit 1
        ;;
esac