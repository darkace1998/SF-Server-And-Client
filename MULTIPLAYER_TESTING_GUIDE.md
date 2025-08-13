# Multiplayer Spawning Fix - Testing Guide

## Issue Fixed
Fixed the issue where when 2+ people join the server, only the first player can see their own character spawning. Other players were not visible to each other.

## Root Cause
The client-side packet routing in `SF_Lidgren/MatchmakingHandlerSocketsPatches.cs` was incorrectly routing `ClientJoined` and `ClientSpawned` packets to the original Steam P2P handler instead of processing them for dedicated server mode.

## Changes Made
1. **Fixed packet routing logic**: Server packets that affect multiplayer state are now properly identified and processed
2. **Added handlers for**:
   - `ClientJoined` packets (type 2) - triggers when other players join
   - `ClientSpawned` packets (type 8) - triggers when other players spawn
   - `MapChange` packets (type 18) - handles map transitions

## How to Test the Fix

### Server Setup
1. Build the server:
   ```bash
   cd SF-Server
   dotnet build
   ```

2. Start server with test credentials:
   ```bash
   dotnet run -- --steam_web_api_token DUMMY_TOKEN --host_steamid 76561198000000000 --port 1337 --max_players 4
   ```
   
   Or for production testing with real Steam API:
   ```bash
   dotnet run -- --steam_web_api_token YOUR_REAL_TOKEN --host_steamid YOUR_STEAM_ID --port 1337 --max_players 4
   ```

### Client Setup
1. Build the client plugin:
   ```bash
   cd SF_Lidgren
   dotnet build
   ```

2. Copy `SF_Lidgren/bin/Debug/net35/SF_Lidgren.dll` to your Stick Fight: The Game `BepInEx/plugins/` directory

3. Start Stick Fight: The Game with BepInEx

### Testing Procedure
1. **Start the server** (see server setup above)

2. **Connect Player 1**:
   - In-game, use the server connection GUI (added by the plugin)
   - Connect to `127.0.0.1:1337` (or your server IP)
   - Player 1 should connect and spawn successfully

3. **Connect Player 2**:
   - On a second instance/computer, connect to the same server
   - Player 2 should connect and both players should be visible

### Expected Behavior (After Fix)
- ✅ **Player 1 joins**: Connects and spawns normally
- ✅ **Player 2 joins**: 
  - Player 1 should see a "Player joined" message in console/logs
  - Player 1 should see Player 2's character appear when Player 2 spawns
- ✅ **Both players visible**: Both players can see each other's characters and movements
- ✅ **Map changes**: Both players transition together when maps change

### Previous Broken Behavior
- ❌ Player 1 would connect and spawn
- ❌ Player 2 would connect but Player 1 couldn't see Player 2
- ❌ Each player could only see themselves

### Debug Information
The fix adds extensive debug logging. Check the game's console output or BepInEx logs for messages like:
- `"Processing ClientJoined packet from server"`
- `"Processing ClientSpawned packet from server"`
- `"Player X joined with Steam ID: Y"`
- `"Triggering spawn for player index: X"`

### Troubleshooting
If players still can't see each other:

1. **Check server logs**: Ensure server is sending `ClientJoined` and `ClientSpawned` packets
2. **Check client logs**: Look for debug messages about packet processing
3. **Verify networking**: Ensure UDP port 1337 is open and accessible
4. **Steam API**: For production testing, ensure valid Steam Web API token

### Additional Testing Scenarios
1. **3-4 Players**: Test with maximum players to ensure scaling works
2. **Player disconnect/reconnect**: Verify rejoining works correctly
3. **Map changes**: Test that all players transition together
4. **Damage/interaction**: Verify players can interact with each other

## Technical Details for Developers

### Key Files Modified
- `SF_Lidgren/MatchmakingHandlerSocketsPatches.cs`: Main packet routing fix

### Packet Types Handled
- `SfPacketType.ClientJoined` (2): Other player joined server
- `SfPacketType.ClientSpawned` (8): Other player spawned in game
- `SfPacketType.MapChange` (18): Map transition events

### Architecture
The fix works by:
1. Intercepting server packets in `ReadMessageMethodPrefix`
2. Identifying important multiplayer packets with `ShouldHandleServerPacket`
3. Processing them with `ProcessServerPacket` and specific handlers
4. Triggering appropriate game methods through reflection to maintain compatibility