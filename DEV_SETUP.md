# Development Environment Setup

## Server Development/Debugging

### Quick Start for Debugging
```bash
cd SF-Server
dotnet run -- --steam_web_api_token YOUR_REAL_TOKEN --host_steamid YOUR_STEAM_ID
```

### Using Debug Configuration
1. Edit `debug_config.json` with your real Steam Web API token and Steam ID
2. Run: `dotnet run -- debug_config.json`

### Development Credentials
- Get Steam Web API Token: https://steamcommunity.com/dev/apikey
- Find your Steam ID: https://steamidfinder.com/ 

### For Local Testing (No Real Clients)
```bash
# This will start server but authentication will fail with real clients
dotnet run -- --steam_web_api_token DUMMY_TOKEN --host_steamid 76561198000000000
```

## Client Development

### Prerequisites
✅ Game assemblies (Assembly-CSharp.dll, etc.) - Already present in SF_Lidgren directory
✅ BepInEx dependencies - Automatically managed via NuGet
✅ Unity Engine references - Already present

### Building Client Plugin
```bash
cd SF_Lidgren
dotnet build
```

### Installing Built Plugin for Testing
1. Build the client: `dotnet build`
2. Copy `bin/Debug/net35/SF_Lidgren.dll` to your game's `BepInEx/plugins/` directory
3. Start Stick Fight: The Game

### Development Workflow
1. Make changes to client code
2. Build: `dotnet build`
3. Copy DLL to game plugins folder
4. Test in game
5. Check BepInEx console for debug output

## Debugging Tips

### Server Debugging
- Enable verbose logging in config
- Monitor `debug_log.txt` for detailed logs
- Use `dotnet run --configuration Debug` for development builds
- Check network connectivity: `netstat -an | grep :1337`

### Client Debugging  
- Check BepInEx console output in game
- Look for SF_Lidgren log messages
- Verify plugin loading in BepInEx logs
- Use Unity's Debug.Log for runtime debugging

### Common Issues
1. **Build Errors**: Make sure all dependencies are restored
2. **Server Auth Errors**: Verify Steam Web API token and Steam ID
3. **Client Load Errors**: Check BepInEx installation and game assemblies
4. **Connection Issues**: Verify firewall and port settings