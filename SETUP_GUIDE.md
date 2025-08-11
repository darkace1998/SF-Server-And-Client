# SF-Server Setup Guide

This comprehensive guide covers everything you need to know about setting up, configuring, and debugging the SF-Server and client plugin.

## 🚀 Quick Start

Get your SF-Server up and running in minutes!

### Server Quick Start

#### Option 1: Docker (Recommended)

```bash
# 1. Clone the repository
git clone https://github.com/darkace1998/SF-Server-And-Client.git
cd SF-Server-And-Client

# 2. Copy environment file and configure
cp .env.example .env
# Edit .env with your Steam Web API token and Steam ID

# 3. Start the server
docker-compose up -d
```

#### Option 2: Native Installation

**Prerequisites**
- .NET 8.0 SDK
- Steam Web API Key ([Get one here](https://steamcommunity.com/dev/apikey))

**Steps**
```bash
# 1. Clone and build
git clone https://github.com/darkace1998/SF-Server-And-Client.git
cd SF-Server-And-Client
./build-server.sh

# 2. Configure and run
cd SF-Server
dotnet run -- --steam_web_api_token YOUR_TOKEN --host_steamid YOUR_STEAMID
```

#### Option 3: Pre-built Release

1. Download the latest release for your platform
2. Extract the archive
3. Copy `server_config.example.json` to `server_config.json`
4. Edit the config file with your Steam credentials
5. Run the startup script for your platform

### Client Quick Start

**Prerequisites**
- Stick Fight: The Game installed
- BepInEx 5.x installed

**Installation**
1. Download the latest `SF_Lidgren.dll` from releases
2. Place it in `BepInEx/plugins/` directory
3. Start the game
4. Press F1 to open the server connection menu
5. Enter your server's IP address and port
6. Click "Connect"

## 🔧 Server Configuration

### Basic Configuration

Create `server_config.json`:
```json
{
  "Port": 1337,
  "SteamWebApiToken": "YOUR_API_TOKEN",
  "HostSteamId": 76561198000000000,
  "MaxPlayers": 4,
  "EnableLogging": true,
  "EnableDebugPacketLogging": false,
  "LogPath": "debug_log.txt",
  "AuthDelayMs": 1000,
  "EnableConsoleOutput": true
}
```

### Environment Variables (Docker)
```bash
SF_STEAM_WEB_API_TOKEN=your_token_here
SF_HOST_STEAMID=76561198000000000
SF_PORT=1337
SF_MAX_PLAYERS=4
```

### Command Line Options

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--port <port>` | Server port | No | 1337 |
| `--steam_web_api_token <token>` | Steam Web API token | Yes | - |
| `--host_steamid <steamid>` | Host Steam ID | Yes | - |
| `--max_players <count>` | Maximum players (1-10) | No | 4 |
| `--config <file>` | Load configuration from file | No | - |

### Example Usage

```bash
# Basic server start
dotnet run -- --steam_web_api_token ABC123 --host_steamid 76561198000000000

# Custom port and player count
dotnet run -- --port 7777 --max_players 8 --steam_web_api_token ABC123 --host_steamid 76561198000000000

# Using config file
dotnet run -- server_config.json --steam_web_api_token ABC123 --host_steamid 76561198000000000
```

## 🎮 Detailed Client Setup

### Prerequisites

1. **Stick Fight: The Game** - Installed via Steam
2. **BepInEx 5.x** - Game modding framework
3. **.NET 3.5 SDK** - For building the client plugin

### Required Game Dependencies

The client plugin requires several game assemblies that must be copied from your Stick Fight installation.

#### Location of Game Files

**Windows (Steam):**
```
C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\
```

**Linux (Steam):**
```
~/.steam/steam/steamapps/common/StickFight/StickFight_Data/Managed/
```

#### Required Files

Copy these files from the game's `Managed` folder to the `SF_Lidgren` project directory:

1. **Assembly-CSharp.dll** - Main game code
2. **Assembly-CSharp-firstpass.dll** - Game framework code
3. **UnityEngine.dll** - Unity engine
4. **Lidgren.Network.dll** - Networking library (if present in game)

#### BepInEx Dependencies

The client also requires BepInEx assemblies. You can either:

**Option A: Manual Installation**
Copy from your BepInEx installation:
- `BepInEx.dll`
- `0Harmony.dll`

**Option B: NuGet Packages (Recommended)**
The project will automatically download BepInEx packages when the NuGet source is available.

### Setting Up the Development Environment

#### 1. Install BepInEx in Game

1. Download BepInEx 5.x from [GitHub](https://github.com/BepInEx/BepInEx/releases)
2. Extract to your Stick Fight game directory
3. Run the game once to generate BepInEx folders

#### 2. Copy Game Dependencies

```bash
# Example for Windows
copy "C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\Assembly-CSharp.dll" "SF_Lidgren\"
copy "C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\Assembly-CSharp-firstpass.dll" "SF_Lidgren\"
copy "C:\Program Files (x86)\Steam\steamapps\common\StickFight\StickFight_Data\Managed\UnityEngine.dll" "SF_Lidgren\"
```

#### 3. Update Project File

Uncomment the reference sections in `SF_Lidgren.csproj`:

```xml
<ItemGroup>
  <Reference Include="Assembly-CSharp">
    <HintPath>Assembly-CSharp.dll</HintPath>
  </Reference>
  <Reference Include="Assembly-CSharp-firstpass">
    <HintPath>Assembly-CSharp-firstpass.dll</HintPath>
  </Reference>
  <Reference Include="Lidgren.Network">
    <HintPath>Lidgren.Network.dll</HintPath>
  </Reference>
  <Reference Include="UnityEngine">
    <HintPath>UnityEngine.dll</HintPath>
  </Reference>
</ItemGroup>
```

#### 4. Build the Client

```bash
cd SF_Lidgren
dotnet restore
dotnet build
```

### Installing the Built Plugin

1. Build the client plugin (see above)
2. Copy `SF_Lidgren.dll` from `bin/Debug/net35/` or `bin/Release/net35/`
3. Place it in `BepInEx/plugins/` in your game directory
4. Start the game

## 🐛 Debugging and Development

### Building the Server

#### Quick Build using provided script
```bash
./build-server.sh
```

#### Manual Build
```bash
cd SF-Server
dotnet restore
dotnet build
```

#### Full Solution (Recommended)
```bash
# Both server and client projects build successfully
dotnet build SF-Server.sln

# Or use the provided build script
./build-debug.sh all
```

#### Quick Development Build
```bash
# Use the provided build script for easier development
./build-debug.sh          # Build everything
./build-debug.sh server    # Build only server
./build-debug.sh client    # Build only client
./build-debug.sh run-server-debug  # Start server with debug credentials
```

### Running the Server in Debug Mode

#### Basic Command
```bash
cd SF-Server
dotnet run -- --steam_web_api_token YOUR_API_KEY --host_steamid YOUR_STEAM_ID
```

#### With Custom Configuration
```bash
cd SF-Server
dotnet run -- --port 7777 --max_players 8 --steam_web_api_token YOUR_API_KEY --host_steamid YOUR_STEAM_ID
```

#### Debug Mode with Verbose Output
```bash
cd SF-Server
dotnet run --configuration Debug -- --steam_web_api_token YOUR_API_KEY --host_steamid YOUR_STEAM_ID
```

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

## 🌐 Network Setup

### Port Configuration
- **Default Port**: 1337 (UDP)
- **Firewall**: Allow UDP traffic on your chosen port
- **Router**: Forward the port to your server if hosting publicly

### Security Considerations
- Use strong Steam Web API tokens
- Consider firewall rules for production
- Monitor connection logs for suspicious activity

## 🔍 Troubleshooting

### Server Issues

**Server Won't Start**
- Verify .NET 8.0 is installed: `dotnet --version`
- Check Steam Web API token is valid
- Ensure port is not in use: `netstat -an | grep 1337`

**Common Server Errors**

**"Steam Web API token is required"**
- Set the token in config file or command line
- Get a token at: https://steamcommunity.com/dev/apikey

**"Port already in use"**
- Change the port in configuration
- Stop other services using the same port

**Build Errors**: The server requires all compilation errors to be fixed. Common issues include:
- Missing extension methods (now fixed in Extensions.cs)
- Read-only property assignments (now fixed with proper constructors)
- Missing method implementations (now fixed in MapManager and ClientManager)

### Client Issues

**Client Can't Connect**
- Verify server is running and accessible
- Check firewall settings on both client and server
- Ensure BepInEx is properly installed
- Check game logs for error messages

**Client Build Errors**

**"Assembly-CSharp could not be found"**
- Ensure you've copied the game assemblies to the project directory
- Check the file paths in the project file

**"BepInEx packages not found"**
- Try building without BepInEx packages first
- Manually copy BepInEx assemblies if needed

**"Target framework not supported"**
- Ensure you have .NET Framework 3.5 installed
- On Linux, ensure Mono is properly configured

**Client Runtime Issues**

**"Plugin not loading"**
- Check BepInEx console for error messages
- Ensure BepInEx is properly installed
- Verify plugin is in the correct directory

**"Game crashes"**
- Check for assembly version mismatches
- Ensure all dependencies are present
- Review BepInEx logs for errors

## 🔧 Development Tips

1. **Keep game files separate** - Don't commit game assemblies to version control
2. **Use symbolic links** - Link to game assemblies instead of copying
3. **Test incrementally** - Build and test small changes frequently
4. **Monitor logs** - BepInEx provides detailed logging for debugging

### IDE Support
- **Visual Studio Code**: Pre-configured launch and task configurations in `.vscode/`
- **Debugging**: Set breakpoints and debug server directly in IDE
- **Build Tasks**: Use Ctrl+Shift+P → "Tasks: Run Task" → "build-all"

### Development Workflow
1. Make changes to server/client code
2. Build with `./build-debug.sh` or IDE
3. For server: Run with debugger or `run-server-debug`
4. For client: Copy DLL to game's `BepInEx/plugins/` and test in-game

## 🚀 Production Deployment

For production deployment:
1. Use Release configuration: `dotnet build --configuration Release`
2. Set proper Steam Web API credentials
3. Configure firewall rules for the server port
4. Consider using a service manager like systemd for automatic startup
5. Monitor server logs for performance and security issues

## ⚡ Quick Commands

```bash
# Start server with custom config
dotnet run -- server_config.json

# Start with specific port
dotnet run -- --port 7777 --steam_web_api_token TOKEN --host_steamid STEAMID

# Build release version
./build-release.sh

# Run with Docker
docker-compose up -d

# View Docker logs
docker-compose logs -f

# Test with dummy parameters (development)
dotnet run -- --steam_web_api_token DUMMY_TOKEN --host_steamid 76561198000000000
```

## 📞 Support

### Getting Help
- Check the [README](README.md) for project overview
- Review this guide for setup and debugging
- Check the GitHub Issues for known problems

### Reporting Issues
When reporting issues, include:
- Server/client version
- Operating system
- Configuration used
- Error messages from logs
- Steps to reproduce

## 🎯 Next Steps

Once your server is running:
1. Test connection with the client plugin
2. Invite friends to play on your server
3. Configure custom settings as needed
4. Monitor server logs for performance
5. Consider setting up automated restarts

## ⚖️ Legal Notice

Game assemblies are copyrighted by Landfall Games. Only use them for development purposes and do not redistribute them.

Enjoy your dedicated Stick Fight server! 🎮