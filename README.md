# SF-Server-And-Client

A custom, dedicated UDP socket server for Stick Fight: The Game using the Lidgren networking library.

This project provides:
- **SF-Server**: A dedicated server for hosting Stick Fight: The Game matches
- **SF_Lidgren**: A BepInEx client plugin to connect to dedicated servers

## 🚀 Quick Start

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
     "EnableConsoleOutput": true
   }
   ```

### Client Setup

1. **Prerequisites**
   - Stick Fight: The Game installed
   - BepInEx 5.x framework installed

2. **Install Required Dependencies**
   Copy the following DLL files from your Stick Fight installation to the `SF_Lidgren/` directory:
   - `Assembly-CSharp.dll` (from `StickFight_Data/Managed/`)
   - `Assembly-CSharp-firstpass.dll` (from `StickFight_Data/Managed/`)
   - `UnityEngine.dll` (from `StickFight_Data/Managed/`)
   - `Lidgren.Network.dll` (from `StickFight_Data/Managed/`)

3. **Build and Install**
   ```bash
   cd SF_Lidgren
   dotnet build
   # Copy the built SF_Lidgren.dll to BepInEx\plugins\ in your game directory
   ```

## 📖 Server Configuration

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
dotnet run -- --config server_config.json --steam_web_api_token ABC123 --host_steamid 76561198000000000
```

## 🔧 Development

### Building the Project

```bash
# Build everything
dotnet build SF-Server.sln

# Or use provided scripts
./build-debug.sh          # Development build
./build-release.sh        # Production build

# Server only
cd SF-Server && dotnet build

# Client only (requires game dependencies)
cd SF_Lidgren && dotnet build
```

### Project Structure

```
SF-Server-And-Client/
├── SF-Server/              # Dedicated server (.NET 8.0)
│   ├── Program.cs          # Entry point and configuration
│   ├── Server.cs           # Main server logic
│   ├── ServerConfig.cs     # Configuration management
│   ├── PacketWorker.cs     # Packet processing
│   ├── ClientManager.cs    # Client connection management
│   └── ...
├── SF_Lidgren/             # Client plugin (.NET 3.5)
│   ├── Plugin.cs           # BepInEx plugin entry point
│   ├── TempGUI.cs          # In-game server browser GUI
│   ├── NetworkUtils.cs     # Networking utilities
│   └── *Patches.cs         # Harmony patches for game integration
├── build-*.sh              # Build scripts
├── Dockerfile              # Container setup
└── docker-compose.yml      # Docker deployment
```

## 🌟 Features

- ✅ Steam Web API authentication
- ✅ UDP networking with Lidgren
- ✅ Player connection management
- ✅ Game packet handling (movement, damage, chat)
- ✅ Graceful server shutdown
- ✅ Cross-platform server support
- ✅ Docker deployment support
- ✅ In-game server browser GUI

## 🐛 Troubleshooting

### Common Issues

1. **Client Build Fails**: Ensure game dependencies are copied to `SF_Lidgren/` directory
2. **Server Connection Failed**: Check firewall settings and Steam Web API token
3. **BepInEx Plugin Not Loading**: Verify BepInEx installation and plugin placement

### Debug Mode

```bash
# Run server with debug logging
./build-debug.sh run-server-debug

# Enable debug packet logging in server_config.json
{
  "EnableDebugPacketLogging": true,
  "LogPath": "debug_log.txt"
}
```

## 🚀 Deployment

### Docker Deployment

```bash
# Build and run with Docker
docker-compose up --build

# Or use provided build script
./build-release.sh docker
```

### Manual Deployment

1. Build the server: `dotnet publish SF-Server -c Release`
2. Copy the published files to your server
3. Configure firewall to allow UDP traffic on your chosen port
4. Set up environment variables or config file with your Steam API credentials

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes and test thoroughly
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## 📋 Requirements

### Server
- .NET 8.0 SDK
- Steam Web API token
- Open UDP port (default: 1337)

### Client
- Stick Fight: The Game
- BepInEx 5.x framework
- Game assembly dependencies (see Client Setup)

## 🔒 Security

- Never commit Steam Web API tokens to version control
- Use environment variables or secure config files for sensitive data
- Consider implementing rate limiting for production deployments
- Review firewall rules and network security

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Lidgren Network Library](https://github.com/lidgren/lidgren-network-gen3) for reliable UDP networking
- [BepInEx](https://github.com/BepInEx/BepInEx) for game modding framework
- [Harmony](https://github.com/pardeike/Harmony) for runtime patching
- The Stick Fight: The Game community

---

**Note**: This is an experimental project and is not affiliated with Landfall Games or the official Stick Fight: The Game.