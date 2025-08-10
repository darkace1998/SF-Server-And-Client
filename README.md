# SF-Server-And-Client

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![BepInEx](https://img.shields.io/badge/BepInEx-5.x-orange.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey.svg)

A custom, dedicated UDP socket server for [**Stick Fight: The Game**](https://store.steampowered.com/app/674940/Stick_Fight_The_Game/) using the Lidgren networking library. Transform the chaotic multiplayer physics brawler into a stable, dedicated server experience with enhanced performance and reliability.

## 🎮 About Stick Fight: The Game

Stick Fight is a physics-based couch/online fighting game where you battle it out as the iconic stick figures from the golden age of the internet. Fight against your friends or random players online in this chaotic physics-based fighter with 100+ weapons and destructible environments.

**Why use a dedicated server?**
- 🚀 **Better Performance**: Reduced lag and improved stability
- 🌍 **24/7 Availability**: Keep your server running around the clock
- ⚙️ **Full Control**: Custom configurations and admin capabilities
- 🔒 **Security**: Enhanced anti-cheat and moderation tools
- 📊 **Monitoring**: Track player statistics and server health

## 📋 Table of Contents

- [🚀 Quick Start](#-quick-start)
- [🏗️ Architecture Overview](#️-architecture-overview)
- [📖 Documentation](#-documentation)
- [🌟 Features](#-features)
- [🔧 Development](#-development)
- [⚙️ Configuration](#️-configuration)
- [🔍 Monitoring & Maintenance](#-monitoring--maintenance)
- [❓ FAQ](#-faq)
- [🐛 Known Issues](#-known-issues)
- [🤝 Contributing](#-contributing)
- [📋 Requirements](#-requirements)
- [🛠 Development & Debugging](#-development--debugging)
- [💬 Community & Support](#-community--support)
- [🔒 Security Notes](#-security-notes)
- [📄 License](#-license)

## 🏗️ Architecture Overview

This project consists of two main components that work together to provide dedicated server functionality:

```
┌─────────────────┐    UDP/Lidgren     ┌─────────────────┐
│   SF-Server     │◄─────────────────► │   Game Client   │
│   (.NET 8.0)    │    Port 1337       │  + SF_Lidgren   │
│                 │                    │   (BepInEx)     │
│ • Steam Auth    │                    │ • Connection    │
│ • Game Logic    │                    │ • GUI Interface │
│ • Map Management│                    │ • Game Patches  │
│ • Player Mgmt   │                    │ • Network Utils │
└─────────────────┘                    └─────────────────┘
         │                                      │
         │                                      │
    ┌────▼────┐                            ┌────▼────┐
    │ Steam   │                            │ Stick   │
    │Web API  │                            │ Fight   │
    │         │                            │ Game    │
    └─────────┘                            └─────────┘
```

**Component Overview:**
- **SF-Server**: Standalone dedicated server handling game logic, player connections, and map management
- **SF_Lidgren**: Client-side BepInEx plugin that connects Stick Fight: The Game to dedicated servers
- **Steam Web API**: Used for player authentication and validation
- **Lidgren Network**: High-performance UDP networking library for real-time game communication

This project provides:
- **SF-Server**: A dedicated server for hosting Stick Fight: The Game matches
- **SF_Lidgren**: A BepInEx client plugin to connect to dedicated servers

## 🚀 Quick Start

**New to SF-Server?** Check out our [Quick Setup Guide](QUICKSTART.md) for the fastest way to get started!

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
     "LogPath": "debug_log.txt",
     "AuthDelayMs": 1000,
     "EnableConsoleOutput": true
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

## ⚙️ Configuration

### Basic Configuration

The server supports multiple configuration methods that can be combined for maximum flexibility:

1. **Configuration File** (Recommended)
2. **Command Line Arguments** 
3. **Environment Variables** (Docker)

### Configuration File

Create `server_config.json` for persistent configuration:

```json
{
  "Port": 1337,
  "SteamWebApiToken": "YOUR_API_TOKEN_HERE",
  "HostSteamId": 76561198000000000,
  "MaxPlayers": 4,
  "EnableLogging": true,
  "LogPath": "debug_log.txt",
  "AuthDelayMs": 1000,
  "EnableConsoleOutput": true
}
```

### Advanced Configuration Examples

#### High-Performance Server (8+ players)
```json
{
  "Port": 1337,
  "SteamWebApiToken": "YOUR_TOKEN",
  "HostSteamId": 76561198000000000,
  "MaxPlayers": 8,
  "EnableLogging": true,
  "LogPath": "high_perf_server.log",
  "AuthDelayMs": 500,
  "EnableConsoleOutput": false,
  "TickRate": 60,
  "ConnectionTimeout": 30000
}
```

#### Development/Testing Server
```json
{
  "Port": 7777,
  "SteamWebApiToken": "DEV_TOKEN",
  "HostSteamId": 76561198000000000,
  "MaxPlayers": 2,
  "EnableLogging": true,
  "LogPath": "dev_debug.log",
  "AuthDelayMs": 100,
  "EnableConsoleOutput": true,
  "DebugMode": true,
  "VerboseLogging": true
}
```

#### Production Server (Public)
```json
{
  "Port": 1337,
  "SteamWebApiToken": "PROD_TOKEN_SECURE",
  "HostSteamId": 76561198000000000,
  "MaxPlayers": 4,
  "EnableLogging": true,
  "LogPath": "/var/log/sf-server/production.log",
  "AuthDelayMs": 1000,
  "EnableConsoleOutput": false,
  "SecurityMode": "strict",
  "RateLimitConnections": true,
  "EnableMetrics": true
}
```

### Environment Variables (Docker/Systemd)

For containerized or service deployments:

```bash
# Required
SF_STEAM_WEB_API_TOKEN=your_token_here
SF_HOST_STEAMID=76561198000000000

# Optional
SF_PORT=1337
SF_MAX_PLAYERS=4
SF_LOG_PATH=/app/logs/server.log
SF_AUTH_DELAY_MS=1000
```

## 📖 Documentation

### Server Command Line Options

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

## 🔍 Monitoring & Maintenance

### Server Health Monitoring

#### Log Monitoring
Monitor these key log events for server health:

```bash
# Monitor connection events
tail -f debug_log.txt | grep "Connection"

# Watch for authentication issues
tail -f debug_log.txt | grep "Steam"

# Monitor performance metrics
tail -f debug_log.txt | grep "Performance\|Memory\|CPU"
```

#### System Resources
Recommended monitoring for production servers:

- **CPU Usage**: Should stay below 50% under normal load
- **Memory Usage**: Monitor for memory leaks, restart if > 1GB
- **Network**: Watch for packet loss or unusual traffic patterns
- **Disk Space**: Ensure adequate space for logs and temporary files

#### Server Status Checks

Create a simple health check script:

```bash
#!/bin/bash
# health_check.sh

SERVER_PORT=1337
if netstat -an | grep -q ":$SERVER_PORT.*LISTEN"; then
    echo "✅ Server is running on port $SERVER_PORT"
    exit 0
else
    echo "❌ Server is not responding on port $SERVER_PORT"
    exit 1
fi
```

### Automated Maintenance

#### Daily Maintenance Script
```bash
#!/bin/bash
# daily_maintenance.sh

LOG_DIR="/path/to/logs"
BACKUP_DIR="/path/to/backups"
DATE=$(date +%Y%m%d)

# Rotate logs
if [ -f "$LOG_DIR/debug_log.txt" ]; then
    cp "$LOG_DIR/debug_log.txt" "$BACKUP_DIR/debug_log_$DATE.txt"
    > "$LOG_DIR/debug_log.txt"  # Clear current log
fi

# Check disk space
DISK_USAGE=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt 80 ]; then
    echo "⚠️ WARNING: Disk usage is at ${DISK_USAGE}%"
fi

# Restart server if memory usage is high
MEMORY_USAGE=$(ps aux | grep 'SF-Server' | grep -v grep | awk '{print $4}')
if (( $(echo "$MEMORY_USAGE > 10.0" | bc -l) )); then
    echo "🔄 Restarting server due to high memory usage: ${MEMORY_USAGE}%"
    systemctl restart sf-server
fi
```

#### Systemd Service (Linux)
Create `/etc/systemd/system/sf-server.service`:

```ini
[Unit]
Description=SF-Server Dedicated Server
After=network.target

[Service]
Type=simple
User=sf-server
WorkingDirectory=/opt/sf-server
ExecStart=/usr/bin/dotnet /opt/sf-server/SF-Server.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

### Performance Optimization

#### Server Tuning
- **Tick Rate**: Adjust based on player count and server specs
- **Buffer Sizes**: Increase for higher player counts
- **Authentication Delay**: Balance security vs. connection speed
- **Logging Level**: Reduce verbosity in production

#### Network Optimization
- **Port Configuration**: Use dedicated ports, avoid common ports
- **Firewall Rules**: Allow only necessary traffic
- **DDoS Protection**: Consider rate limiting and connection filtering
- **Quality of Service**: Prioritize game traffic if possible

### Backup & Recovery

#### Configuration Backup
```bash
# Backup server configuration
cp server_config.json "config_backup_$(date +%Y%m%d).json"

# Version control for configurations
git add server_config.json
git commit -m "Update server configuration"
```

#### Server State Recovery
- **Player Data**: Currently stored in memory, consider persistent storage
- **Map States**: Automatic reset on restart
- **Connection States**: Clients auto-reconnect on server restart

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

## ❓ FAQ

### General Questions

**Q: What is Stick Fight: The Game?**
A: Stick Fight is a physics-based multiplayer fighting game where players control stick figures in chaotic battles with destructible environments and 100+ weapons.

**Q: Why use a dedicated server instead of peer-to-peer?**
A: Dedicated servers provide better performance, reduced lag, 24/7 availability, and enhanced anti-cheat capabilities.

**Q: Can I run this on a VPS/cloud server?**
A: Yes! The server runs on Linux, Windows, and macOS. Popular choices include DigitalOcean, AWS, Google Cloud, and Azure.

### Setup & Installation

**Q: What do I need to get started?**
A: You need .NET 8.0, a Steam Web API key, and your Steam ID. For clients, they need Stick Fight: The Game and BepInEx installed.

**Q: How do I get a Steam Web API key?**
A: Visit [steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey), sign in with your Steam account, and register for a key.

**Q: How do I find my Steam ID?**
A: Use [steamidfinder.com](https://steamidfinder.com/) or check your Steam profile URL. You need the 64-bit Steam ID (starts with 765611...).

**Q: Can I run multiple servers on the same machine?**
A: Yes, use different ports for each server instance.

### Client Connection Issues

**Q: Client shows "Connection Failed" - what's wrong?**
A: Check these common issues:
- Server is running and accessible
- Correct IP address and port
- Firewall not blocking the connection
- BepInEx plugin installed correctly
- Steam authentication working

**Q: The plugin doesn't load in the game**
A: Verify:
- BepInEx is properly installed (run game once after installing)
- `SF_Lidgren.dll` is in `BepInEx/plugins/` folder
- Check BepInEx console for error messages
- Ensure game version compatibility

**Q: How do I open the connection GUI in game?**
A: Press **F1** while in the game to toggle the server connection interface.

### Server Administration

**Q: How many players can my server handle?**
A: Default is 4 players, configurable up to 10. Actual capacity depends on your server specifications and network bandwidth.

**Q: Can I set up admin commands?**
A: The current version focuses on core functionality. Admin features are planned for future releases.

**Q: How do I change maps on my server?**
A: Map changes are handled automatically by the game client through the server. Custom map support is planned.

**Q: How do I restart the server remotely?**
A: Use systemd on Linux (`systemctl restart sf-server`) or set up remote desktop/SSH access for manual restart.

### Performance & Troubleshooting

**Q: Server is using too much CPU/memory**
A: Try:
- Reducing max players
- Disabling verbose logging
- Checking for connection spam (rate limiting)
- Restarting the server periodically

**Q: Players experience lag despite good internet**
A: Check:
- Server location relative to players
- Server hardware specifications
- Network configuration and QoS settings
- Other processes competing for resources

**Q: How do I backup my server configuration?**
A: Simply copy your `server_config.json` file. Consider using version control (git) for configuration management.

### Development & Customization

**Q: Can I modify the server code?**
A: Yes! The project is open-source under MIT license. See the [Contributing](#-contributing) section for guidelines.

**Q: How do I build the client plugin from source?**
A: See [CLIENT_SETUP.md](CLIENT_SETUP.md) for detailed instructions on setting up the development environment.

**Q: Can I add custom features to the server?**
A: Absolutely! The codebase is designed to be extensible. Consider contributing your improvements back to the project.

### Security & Privacy

**Q: Is it safe to run a public server?**
A: The server includes security features like input validation and Steam authentication. Follow the [Security Notes](#-security-notes) for best practices.

**Q: Do I need to open ports in my firewall?**
A: Yes, you need to allow incoming UDP traffic on your configured server port (default 1337).

**Q: Can players see my Steam API key?**
A: No, the Steam API key is only used server-side for authentication and is never transmitted to clients.

## 🐛 Known Issues

1. **Client Plugin Dependencies**: The client plugin requires game assemblies that must be manually copied from the Stick Fight installation
2. **BepInEx Package Source**: Some build environments may have issues accessing the BepInEx NuGet source
3. **Limited Map Support**: Currently only supports the base game maps
4. **Authentication Delays**: First-time connections may take longer due to Steam API validation
5. **Network Firewall Issues**: Some corporate/university firewalls may block UDP traffic on custom ports

### Workarounds

**For Client Dependencies:**
- Follow the detailed instructions in [CLIENT_SETUP.md](CLIENT_SETUP.md)
- Ensure all game assemblies are properly copied to the project directory

**For BepInEx Issues:**
- Use alternative NuGet sources or manual assembly references
- Check the BepInEx installation guide for your specific environment

**For Map Support:**
- Custom map support is planned for future releases
- Currently supports all standard game maps and transitions

**For Network Issues:**
- Try alternative ports (7777, 8888) if default port 1337 is blocked
- Contact network administrators for UDP port allowlisting
- Consider VPN solutions for restrictive network environments

## 🤝 Contributing

We welcome contributions from the community! Whether you're fixing bugs, adding features, improving documentation, or helping with testing, your contributions make this project better for everyone.

### How to Contribute

#### 🐛 Reporting Bugs
1. **Search existing issues** to avoid duplicates
2. **Use the bug report template** when creating new issues
3. **Include detailed information**:
   - Server version and configuration
   - Operating system and .NET version
   - Steps to reproduce the issue
   - Expected vs. actual behavior
   - Relevant log files or error messages

#### 💡 Suggesting Features
1. **Check the project roadmap** and existing feature requests
2. **Open a GitHub Discussion** to propose new features
3. **Describe the use case** and potential implementation
4. **Gather community feedback** before starting development

#### 💻 Code Contributions

**Getting Started:**
1. **Fork the repository** to your GitHub account
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/SF-Server-And-Client.git
   cd SF-Server-And-Client
   ```
3. **Create a feature branch**:
   ```bash
   git checkout -b feature/amazing-feature
   ```

**Development Workflow:**
1. **Set up the development environment** using [DEV_SETUP.md](DEV_SETUP.md)
2. **Make your changes** following the coding standards
3. **Test thoroughly**:
   - Build both server and client projects
   - Test with actual game clients
   - Verify no regressions in existing functionality
4. **Update documentation** if needed
5. **Write clear commit messages**:
   ```
   feat: add player statistics tracking
   
   - Implement player stats collection
   - Add statistics API endpoints
   - Update configuration for stats storage
   
   Closes #123
   ```

**Code Standards:**
- Follow C# coding conventions and .NET best practices
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Include unit tests for new functionality where applicable
- Ensure all existing tests pass

**Pull Request Process:**
1. **Push your changes** to your fork:
   ```bash
   git push origin feature/amazing-feature
   ```
2. **Open a Pull Request** with:
   - Clear title and description
   - Reference to related issues
   - Screenshots or demos for UI changes
   - Summary of testing performed
3. **Respond to feedback** and make requested changes
4. **Celebrate** when your PR is merged! 🎉

#### 📝 Documentation Contributions
- Fix typos and improve clarity
- Add examples and use cases
- Update setup guides for new features
- Translate documentation to other languages
- Create video tutorials or guides

#### 🧪 Testing & Quality Assurance
- Test new releases with different configurations
- Report compatibility issues with game updates
- Verify documentation accuracy
- Performance testing with various player counts

### Development Environment

**Prerequisites:**
- .NET 8.0 SDK for server development
- .NET Framework 3.5 for client plugin development
- Git for version control
- IDE (Visual Studio, VS Code, or JetBrains Rider)

**Quick Setup:**
```bash
# Clone and build
git clone https://github.com/darkace1998/SF-Server-And-Client.git
cd SF-Server-And-Client
./build-debug.sh

# Run tests
dotnet test

# Start development server
./build-debug.sh run-server-debug
```

### Coding Guidelines

**General Principles:**
- Write clean, readable, and maintainable code
- Follow SOLID principles and design patterns
- Prefer composition over inheritance
- Use dependency injection where appropriate
- Handle errors gracefully with proper logging

**Specific Standards:**
- Use PascalCase for public members
- Use camelCase for private fields and local variables
- Prefix private fields with underscore (`_field`)
- Use async/await for I/O operations
- Validate all inputs, especially from network sources
- Add comprehensive logging for debugging

**Security Considerations:**
- Validate and sanitize all user inputs
- Use parameterized queries for database operations
- Implement proper authentication and authorization
- Follow the principle of least privilege
- Regular security reviews for network-facing code

### Release Process

**Version Numbering:**
We follow [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

**Release Checklist:**
- [ ] All tests passing
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] Version numbers bumped
- [ ] Security review completed
- [ ] Multi-platform testing performed

### Recognition

Contributors are recognized in several ways:
- Listed in repository contributors
- Mentioned in release notes for significant contributions
- Added to CONTRIBUTORS.md file (if maintained)
- GitHub contribution graph and profile recognition

### Code of Conduct

This project adheres to a code of conduct adapted from the [Contributor Covenant](https://www.contributor-covenant.org/). By participating, you agree to uphold this code.

**In Summary:**
- Be welcoming and inclusive
- Be respectful of differing viewpoints
- Accept constructive criticism gracefully
- Focus on what's best for the community
- Show empathy towards other community members

### Getting Help with Contributing

If you need help contributing:
- Ask questions in GitHub Discussions
- Review existing PRs for examples
- Check the [Development Setup Guide](DEV_SETUP.md)
- Reach out to maintainers for guidance

**Thank you for contributing to SF-Server! Every contribution, no matter how small, helps make this project better for the entire community.** 🙏

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

See [DEV_SETUP.md](DEV_SETUP.md) for detailed development instructions.

## 💬 Community & Support

### Getting Help

1. **📖 Documentation First**: Check the comprehensive guides in this repository:
   - [Quick Setup Guide](QUICKSTART.md) - Get started in minutes
   - [Client Setup Guide](CLIENT_SETUP.md) - Detailed client configuration
   - [Debug Guide](DEBUG_GUIDE.md) - Troubleshooting and debugging
   - [Development Setup](DEV_SETUP.md) - Developer environment setup

2. **🐛 Issues & Bug Reports**: [GitHub Issues](https://github.com/darkace1998/SF-Server-And-Client/issues)
   - Search existing issues before creating new ones
   - Use issue templates for bug reports and feature requests
   - Include server logs, configuration, and steps to reproduce

3. **💡 Feature Requests**: [GitHub Discussions](https://github.com/darkace1998/SF-Server-And-Client/discussions)
   - Propose new features and enhancements
   - Discuss implementation ideas with the community
   - Vote on proposed features

### Community Guidelines

When participating in the community:

- **Be respectful**: Treat everyone with kindness and professionalism
- **Be helpful**: Share your knowledge and assist others when possible
- **Be constructive**: Provide actionable feedback and suggestions
- **Follow rules**: Adhere to GitHub's community guidelines
- **Stay on topic**: Keep discussions relevant to the SF-Server project

### Contributing to the Project

We welcome contributions! See our [Contributing Guidelines](#-contributing) for details on:

- Code contributions and pull requests
- Documentation improvements
- Testing and quality assurance
- Community support and moderation

### Useful Resources

#### Official Links
- **Repository**: [GitHub - SF-Server-And-Client](https://github.com/darkace1998/SF-Server-And-Client)
- **Releases**: [Latest Downloads](https://github.com/darkace1998/SF-Server-And-Client/releases)
- **License**: [MIT License](LICENSE)

#### Related Projects
- **Stick Fight: The Game**: [Steam Store Page](https://store.steampowered.com/app/674940/Stick_Fight_The_Game/)
- **BepInEx**: [Game Modding Framework](https://github.com/BepInEx/BepInEx)
- **Lidgren Network**: [Networking Library](https://github.com/lidgren/lidgren-network-gen3)
- **Steam Web API**: [Developer Documentation](https://steamcommunity.com/dev)

#### External Communities
- **Stick Fight Discord**: Check the official game's community channels
- **BepInEx Discord**: For plugin development questions
- **r/StickFight**: Reddit community for the game

### Support the Project

Help improve SF-Server by:

- ⭐ **Starring the repository** to show your support
- 🐛 **Reporting bugs** and issues you encounter
- 📝 **Contributing documentation** improvements
- 💻 **Submitting code** improvements and new features
- 🗣️ **Spreading the word** about the project
- 💰 **Sponsoring development** (if sponsor options are available)

## 🔒 Security Notes

- Never commit your Steam Web API token to version control
- Use configuration files or environment variables for sensitive data
- Consider firewall rules for production deployments

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### What This Means

The MIT License is a permissive open-source license that allows you to:
- ✅ **Use** the software for any purpose (commercial or non-commercial)
- ✅ **Modify** the code to suit your needs
- ✅ **Distribute** copies of the software
- ✅ **Sublicense** and sell copies of the software

**Requirements:**
- Include the original license and copyright notice
- Include the license when distributing the software

**No Warranty:**
- The software is provided "as is" without warranty
- Authors are not liable for any damages or issues

## 📋 Documentation

- [Quick Setup Guide](QUICKSTART.md) - Get started in minutes
- [Client Setup Guide](CLIENT_SETUP.md) - Detailed client configuration
- [Development Setup](DEV_SETUP.md) - Developer environment setup
- [Debug Guide](DEBUG_GUIDE.md) - Troubleshooting and debugging
- [Security Analysis](SECURITY_ANALYSIS.md) - Security features and considerations
- [Implementation Summary](IMPLEMENTATION_SUMMARY.md) - Technical implementation details
- [Changelog](CHANGELOG.md) - Version history and improvements
- [Docker Guide](docker-compose.yml) - Container deployment

## 🙏 Acknowledgments

### Core Dependencies
- **[Lidgren Network Library](https://github.com/lidgren/lidgren-network-gen3)** - High-performance UDP networking foundation
- **[BepInEx](https://github.com/BepInEx/BepInEx)** - Unity game modding framework that makes client plugins possible
- **[Harmony](https://github.com/pardeike/Harmony)** - Runtime patching library for seamless game integration
- **[.NET](https://dotnet.microsoft.com/)** - Cross-platform development framework

### Development Tools
- **[Steam Web API](https://steamcommunity.com/dev)** - Player authentication and validation
- **[GitHub Actions](https://github.com/features/actions)** - Continuous integration and deployment
- **[Docker](https://www.docker.com/)** - Containerization and deployment platform

### Special Thanks
- **Landfall Games** - Creators of Stick Fight: The Game
- **The Stick Fight Community** - For their support and enthusiasm
- **Open Source Contributors** - Everyone who has contributed code, documentation, and ideas
- **Beta Testers** - Community members who helped test and improve the server

### Inspiration & Research
This project was inspired by the need for stable, dedicated server infrastructure for Stick Fight: The Game. Special recognition goes to:
- Community server administrators who identified the need
- Network programming enthusiasts who provided technical guidance
- Game modding communities for sharing knowledge and best practices

### Legal Notice

**Important:** This project is an independent, community-driven effort and is **not affiliated with Landfall Games** or the official Stick Fight: The Game development team.

- Stick Fight: The Game is a trademark of Landfall Games
- This server implementation is a reverse-engineered, compatible solution
- Game assets and proprietary code remain the property of Landfall Games
- This project operates under fair use for interoperability purposes

**Disclaimer:** This software is provided for educational and interoperability purposes. Users are responsible for compliance with game terms of service and applicable laws.

---

## 🚀 Ready to Get Started?

1. **New Users**: Start with the [Quick Setup Guide](QUICKSTART.md)
2. **Developers**: Check out the [Development Setup](DEV_SETUP.md)
3. **Questions**: Visit our [FAQ](#-faq) or [Community](#-community--support) section
4. **Issues**: Report bugs on [GitHub Issues](https://github.com/darkace1998/SF-Server-And-Client/issues)

**Happy Gaming!** 🎮 Transform your Stick Fight experience with dedicated server power.

---

*Last updated: January 2025 | Project Version: 1.0.0+ | Maintained by the SF-Server Community*