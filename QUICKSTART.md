# Quick Setup Guide

Get your SF-Server up and running in minutes!

## 🚀 Server Quick Start

### Option 1: Docker (Recommended)

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

### Option 2: Native Installation

#### Prerequisites
- .NET 8.0 SDK
- Steam Web API Key ([Get one here](https://steamcommunity.com/dev/apikey))

#### Steps
```bash
# 1. Clone and build
git clone https://github.com/darkace1998/SF-Server-And-Client.git
cd SF-Server-And-Client
./build-server.sh

# 2. Configure and run
cd SF-Server
dotnet run -- --steam_web_api_token YOUR_TOKEN --host_steamid YOUR_STEAMID
```

### Option 3: Pre-built Release

1. Download the latest release for your platform
2. Extract the archive
3. Copy `server_config.example.json` to `server_config.json`
4. Edit the config file with your Steam credentials
5. Run the startup script for your platform

## 🎮 Client Quick Start

### Prerequisites
- Stick Fight: The Game installed
- BepInEx 5.x installed

### Installation
1. Download the latest `SF_Lidgren.dll` from releases
2. Place it in `BepInEx/plugins/` directory
3. Start the game
4. Press F1 to open the server connection menu
5. Enter your server's IP address and port
6. Click "Connect"

## 🔧 Configuration

### Server Configuration
Create `server_config.json`:
```json
{
  "Port": 1337,
  "SteamWebApiToken": "YOUR_API_TOKEN",
  "HostSteamId": 76561198000000000,
  "MaxPlayers": 4,
  "EnableLogging": true
}
```

### Environment Variables (Docker)
```bash
SF_STEAM_WEB_API_TOKEN=your_token_here
SF_HOST_STEAMID=76561198000000000
SF_PORT=1337
SF_MAX_PLAYERS=4
```

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

### Server Won't Start
- Verify .NET 8.0 is installed: `dotnet --version`
- Check Steam Web API token is valid
- Ensure port is not in use: `netstat -an | grep 1337`

### Client Can't Connect
- Verify server is running and accessible
- Check firewall settings on both client and server
- Ensure BepInEx is properly installed
- Check game logs for error messages

### Common Issues

**"Steam Web API token is required"**
- Set the token in config file or command line
- Get a token at: https://steamcommunity.com/dev/apikey

**"Port already in use"**
- Change the port in configuration
- Stop other services using the same port

**"Client plugin not loading"**
- Verify BepInEx installation
- Check that `SF_Lidgren.dll` is in `BepInEx/plugins/`
- Review BepInEx console for error messages

## 📞 Support

### Getting Help
- Check the [README](README.md) for detailed documentation
- Review the [Client Setup Guide](CLIENT_SETUP.md) for client issues
- Check the [Changelog](CHANGELOG.md) for recent updates

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
```

Enjoy your dedicated Stick Fight server! 🎮