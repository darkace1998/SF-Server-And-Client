# SF Server Debug Guide

This guide provides instructions for building and debugging the SF Server project.

## Prerequisites

- .NET 8.0 SDK or later
- Steam Web API Key ([Get one here](https://steamcommunity.com/dev/apikey))
- Your Steam ID (convert from Steam profile URL)

## Building the Server

### Quick Build using provided script
```bash
./build-server.sh
```

### Manual Build
```bash
cd SF-Server
dotnet restore
dotnet build
```

## Running the Server

### Basic Command
```bash
cd SF-Server
dotnet run -- --steam_web_api_token YOUR_API_KEY --host_steamid YOUR_STEAM_ID
```

### With Custom Configuration
```bash
cd SF-Server
dotnet run -- --port 7777 --max_players 8 --steam_web_api_token YOUR_API_KEY --host_steamid YOUR_STEAM_ID
```

### Debug Mode with Verbose Output
```bash
cd SF-Server
dotnet run --configuration Debug -- --steam_web_api_token YOUR_API_KEY --host_steamid YOUR_STEAM_ID
```

## Configuration Options

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `--steam_web_api_token` | Yes | - | Steam Web API token for authentication |
| `--host_steamid` | Yes | - | Steam ID of the server host |
| `--port` | No | 1337 | UDP port for server to listen on |
| `--max_players` | No | 4 | Maximum number of players (1-10) |
| `--config` | No | - | Load configuration from JSON file |

## Debugging

### Common Issues

1. **Build Errors**: The server requires all compilation errors to be fixed. Common issues include:
   - Missing extension methods (now fixed in Extensions.cs)
   - Read-only property assignments (now fixed with proper constructors)
   - Missing method implementations (now fixed in MapManager and ClientManager)

2. **Steam API Connection**: Server requires valid Steam Web API credentials to authenticate players.

3. **Network Issues**: Ensure the specified port is available and not blocked by firewall.

### Debugging Steps

1. **Check Configuration**:
   ```bash
   dotnet run -- --help
   ```

2. **Test with Dummy Parameters** (for development):
   ```bash
   dotnet run -- --steam_web_api_token DUMMY_TOKEN --host_steamid 76561198000000000
   ```
   Note: This will start the server but authentication will fail with real clients.

3. **Enable Verbose Logging**: Check the server output for connection attempts and errors.

4. **Check Network Connectivity**: Verify the port is accessible:
   ```bash
   netstat -an | grep :1337
   ```

### Development Debugging

For development and debugging:

1. **Use IDE Debugger**: Set breakpoints in Visual Studio/VS Code
2. **Console Output**: Server prints status messages and connection attempts
3. **Log Files**: Check debug_log.txt if logging is enabled
4. **Network Monitoring**: Use tools like Wireshark to monitor UDP traffic

## Configuration File Example

Create `server_config.json`:
```json
{
  "Port": 1337,
  "SteamWebApiToken": "",
  "HostSteamId": 0,
  "MaxPlayers": 4,
  "EnableLogging": true,
  "LogPath": "debug_log.txt",
  "AuthDelayMs": 1000,
  "EnableConsoleOutput": true
}
```

Then run:
```bash
dotnet run -- server_config.json
```

## Compilation Fixes Applied

The following issues were resolved to make the server buildable:

1. **Added Extension Methods** (`Extensions.cs`):
   - `string.Truncate(int maxLength)`
   - `NetIncomingMessage.GetSenderIP()`

2. **Fixed Read-only Property Assignments**:
   - Changed object initializer syntax to constructor calls for immutable structs
   - Fixed Vector2, Vector3, PositionPackage, WeaponPackage, and MapData creation

3. **Added Missing Methods**:
   - `ClientManager.Clients` property
   - `MapManager.GetLobbyMap()` method

4. **Fixed Static Method Access**:
   - Changed instance method call to static method call for `MapManager.ValidateMapChange()`

## Production Deployment

For production deployment:
1. Use Release configuration: `dotnet build --configuration Release`
2. Set proper Steam Web API credentials
3. Configure firewall rules for the server port
4. Consider using a service manager like systemd for automatic startup
5. Monitor server logs for performance and security issues