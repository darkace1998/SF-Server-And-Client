# SF-Server-And-Client

A custom, dedicated UDP socket server for Stick Fight: The Game using the networking library Lidgren.

This project provides:
- **SF-Server**: A dedicated server for hosting Stick Fight: The Game matches
- **SF_Lidgren**: A BepInEx client plugin to connect to dedicated servers

## 🚀 Quick Start

**New to SF-Server?** Check out our [Complete Setup Guide](SETUP_GUIDE.md) for the fastest way to get started!

### Server Setup

1. **Prerequisites**
   - .NET 8.0 or later
   - Steam Web API Key ([Get one here](https://steamcommunity.com/dev/apikey))

2. **Running the Server**
   ```bash
   # Build and run the server
   cd SF-Server
   dotnet build
   dotnet run -- --steam_web_api_token YOUR_API_KEY --host_steamid YOUR_STEAM_ID
   ```

3. **Configuration File (Optional)**
   Create `server_config.json`:
   ```json
   {
     "Port": 1337,
     "SteamWebApiToken": "",
     "HostSteamId": 0,
     "MaxPlayers": 4,
     "EnableLogging": true,
     "EnableDebugPacketLogging": false,
     "LogPath": "debug_log.txt",
     "AuthDelayMs": 1000,
     "EnableConsoleOutput": true,
     "AutoStartSinglePlayer": true
   }
   ```

### Client Setup

1. **Prerequisites**
   - Stick Fight: The Game installed
   - BepInEx 5.x installed

2. **Installation**
   - Download the latest client plugin from releases
   - Place `SF_Lidgren.dll` in `BepInEx\plugins` directory
   - Start the game and join servers via the in-game GUI

## 📖 Documentation

### Server Command Line Options

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--port <port>` | Server port | No | 1337 |
| `--steam_web_api_token <token>` | Steam Web API token | Yes | - |
| `--host_steamid <steamid>` | Host Steam ID | Yes | - |
| `--max_players <count>` | Maximum players (1-10) | No | 4 |
| `--auto_start_single_player <true/false>` | Auto-start single players into maps | No | true |
| `--config <file>` | Load configuration from file | No | - |

### Example Usage

```bash
# Basic server start
dotnet run -- --steam_web_api_token ABC123 --host_steamid 76561198000000000

# Custom port and player count
dotnet run -- --port 7777 --max_players 8 --steam_web_api_token ABC123 --host_steamid 76561198000000000

# Disable auto-start for single players (keep them in lobby)
dotnet run -- --auto_start_single_player false --steam_web_api_token ABC123 --host_steamid 76561198000000000

# Using config file
dotnet run -- server_config.json --steam_web_api_token ABC123 --host_steamid 76561198000000000
```

## 🔧 Development

### Building the Project

#### Server Only
```bash
cd SF-Server
dotnet restore
dotnet build
```

#### Client Only
```bash
cd SF_Lidgren
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

### Project Structure

```
SF-Server-And-Client/
├── SF-Server/              # Dedicated server project (.NET 8.0)
│   ├── Program.cs          # Entry point and configuration
│   ├── Server.cs           # Main server logic
│   ├── ServerConfig.cs     # Configuration management
│   ├── ShutdownHandler.cs  # Graceful shutdown handling
│   ├── PacketWorker.cs     # Packet processing
│   ├── ClientManager.cs    # Client connection management
│   └── ...
├── SF_Lidgren/             # Client plugin project (.NET 3.5)
│   ├── Plugin.cs           # BepInEx plugin entry point
│   ├── TempGUI.cs          # In-game server connection GUI
│   ├── NetworkUtils.cs     # Networking utilities
│   └── *Patches.cs         # Harmony patches for game integration
└── README.md
```

## 🌟 Features

### Current Features
- ✅ Steam Web API authentication
- ✅ UDP networking with Lidgren
- ✅ Player connection management
- ✅ Basic game packet handling (movement, damage, chat)
- ✅ Graceful server shutdown
- ✅ Configuration management
- ✅ Cross-platform server support

### Planned Features
- 🔄 Custom map support
- 🔄 Advanced server administration
- 🔄 Player statistics tracking
- 🔄 Enhanced security features
- 🔄 Web-based server management
- 🔄 Docker deployment

## 🐛 Known Issues

1. **Client Plugin Dependencies**: The client plugin requires game assemblies that must be manually copied from the Stick Fight installation
2. **BepInEx Package Source**: Some build environments may have issues accessing the BepInEx NuGet source
3. **Limited Map Support**: Currently only supports the base game maps

## ✅ Fixed Issues

- **Connection Issues**: Fixed client connection hanging - see [CONNECTION_FIX.md](CONNECTION_FIX.md) for details
- **Authentication Errors**: Improved error handling for Steam Web API authentication
- **Missing Message Types**: Added support for all required Lidgren network message types

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Test thoroughly
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## 📋 Requirements

### Server Requirements
- .NET 8.0 SDK
- Network connectivity for Steam Web API
- Open UDP port (default: 1337)

### Client Requirements
- Stick Fight: The Game
- BepInEx 5.x
- Game assemblies (✅ **Already included** in SF_Lidgren directory)

## 🛠 Development & Debugging

### Quick Development Setup
1. **Clone and Build**: `git clone <repo> && cd SF-Server-And-Client && ./build-debug.sh`
2. **Debug Server**: `./build-debug.sh run-server-debug` (uses dummy credentials)
3. **Build Client**: Client DLL automatically built to `SF_Lidgren/bin/Debug/net35/SF_Lidgren.dll`

### IDE Support
- **Visual Studio Code**: Pre-configured launch and task configurations in `.vscode/`
- **Debugging**: Set breakpoints and debug server directly in IDE
- **Build Tasks**: Use Ctrl+Shift+P → "Tasks: Run Task" → "build-all"

### Development Workflow
1. Make changes to server/client code
2. Build with `./build-debug.sh` or IDE
3. For server: Run with debugger or `run-server-debug`
4. For client: Copy DLL to game's `BepInEx/plugins/` and test in-game

See [SETUP_GUIDE.md](SETUP_GUIDE.md) for detailed development instructions.

## 🔒 Security Notes

- Never commit your Steam Web API token to version control
- Use configuration files or environment variables for sensitive data
- Consider firewall rules for production deployments

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📋 Documentation

- [Complete Setup Guide](SETUP_GUIDE.md) - Everything you need to get started
- [Docker Guide](docker-compose.yml) - Container deployment

## 🙏 Acknowledgments

- [Lidgren Network Library](https://github.com/lidgren/lidgren-network-gen3) for networking
- [BepInEx](https://github.com/BepInEx/BepInEx) for game modding framework
- [Harmony](https://github.com/pardeike/Harmony) for runtime patching
- The Stick Fight: The Game community

---

**Note**: This is an experimental project and is not affiliated with Landfall Games or the official Stick Fight: The Game.