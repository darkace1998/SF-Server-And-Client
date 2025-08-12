#!/bin/bash

# SF-Server Connection Test Script
# This script tests the connection between client and server components

set -e

echo "=== SF-Server Connection Test ==="
echo "This script tests that the connection issue has been fixed."
echo ""

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$PROJECT_ROOT/SF-Server"

# Build the project
echo "📦 Building project..."
cd "$PROJECT_ROOT"
dotnet build SF-Server.sln > /dev/null 2>&1
echo "✅ Build successful"

# Create test client
echo "🔧 Creating test client..."
cd /tmp
rm -rf ConnectionTest
dotnet new console -n ConnectionTest --force > /dev/null 2>&1
cd ConnectionTest

cat > Program.cs << 'EOF'
using System;
using System.Threading;
using Lidgren.Network;

class ConnectionTest
{
    public static void Main()
    {
        Console.WriteLine("Testing SF-Server connection...");
        
        var config = new NetPeerConfiguration("monky.SF_Lidgren");
        config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
        config.EnableMessageType(NetIncomingMessageType.StatusChanged);
        config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
        
        var client = new NetClient(config);
        client.Start();
        
        var authMessage = client.CreateMessage();
        authMessage.Write("TEST_TICKET");
        
        var connection = client.Connect("localhost", 1337, authMessage);
        
        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(1000);
            
            NetIncomingMessage msg;
            while ((msg = client.ReadMessage()) != null)
            {
                if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var status = (NetConnectionStatus)msg.ReadByte();
                    if (status == NetConnectionStatus.Connected)
                    {
                        Console.WriteLine("✅ Connection successful!");
                        client.Shutdown("Test complete");
                        return;
                    }
                }
                client.Recycle(msg);
            }
        }
        
        Console.WriteLine("❌ Connection failed");
        client.Shutdown("Test failed");
    }
}
EOF

dotnet add package Lidgren.Network --version 1.0.2 > /dev/null 2>&1

# Start server in background
echo "🚀 Starting test server..."
cd "$SERVER_DIR"
dotnet run -- --steam_web_api_token DUMMY_TOKEN --host_steamid 76561198000000000 --port 1337 > /dev/null 2>&1 &
SERVER_PID=$!

# Give server time to start
sleep 2

# Test connection
echo "🔗 Testing connection..."
cd /tmp/ConnectionTest
timeout 10s dotnet run || echo "❌ Connection test failed"

# Cleanup
echo "🧹 Cleaning up..."
kill $SERVER_PID 2>/dev/null || true
cd /tmp
rm -rf ConnectionTest

echo ""
echo "=== Test Complete ==="
echo "If you see '✅ Connection successful!' above, the fix is working!"
echo ""
echo "Next steps for users:"
echo "1. Get a real Steam Web API key from: https://steamcommunity.com/dev/apikey"
echo "2. Replace DUMMY_TOKEN with your real token"
echo "3. Install BepInEx in your Stick Fight game"
echo "4. Copy SF_Lidgren.dll to BepInEx/plugins/"
echo "5. Start your server and connect from the game using F1 menu"