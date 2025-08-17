using System.Text.Json;

namespace SFServer;

/// <summary>
/// Configuration class for the SF Server
/// </summary>
public class ServerConfig
{
    public int Port { get; set; } = 1337;
    public string SteamWebApiToken { get; set; } = "";
    public ulong HostSteamId { get; set; } = 0;
    public int MaxPlayers { get; set; } = 4;
    public bool EnableLogging { get; set; } = true;
    public bool EnableDebugPacketLogging { get; set; } = false;
    public string LogPath { get; set; } = "debug_log.txt";
    public int AuthDelayMs { get; set; } = 1000;
    public bool EnableConsoleOutput { get; set; } = true;
    
    /// <summary>
    /// Network game options for server configuration
    /// </summary>
    public NetworkOptions GameOptions { get; set; } = NetworkOptions.Default;
    
    /// <summary>
    /// Whether single players should automatically load into a map instead of waiting in lobby
    /// </summary>
    public bool AutoStartSinglePlayer { get; set; } = true;
    
    /// <summary>
    /// Whether to automatically start the game when minimum players join (2+)
    /// </summary>
    public bool AutoStartMultiplayer { get; set; } = true;
    
    /// <summary>
    /// Minimum number of players required to auto-start a multiplayer game
    /// </summary>
    public int MinPlayersForAutoStart { get; set; } = 2;

    /// <summary>
    /// Load configuration from a JSON file
    /// </summary>
    /// <param name="configPath">Path to the configuration file</param>
    /// <returns>ServerConfig instance</returns>
    public static ServerConfig LoadFromFile(string configPath)
    {
        if (!File.Exists(configPath))
        {
            var defaultConfig = new ServerConfig();
            defaultConfig.SaveToFile(configPath);
            Console.WriteLine($"Created default configuration file at: {configPath}");
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<ServerConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            });
            return config ?? new ServerConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load configuration from {configPath}: {ex.Message}");
            Console.WriteLine("Using default configuration...");
            return new ServerConfig();
        }
    }

    /// <summary>
    /// Save configuration to a JSON file
    /// </summary>
    /// <param name="configPath">Path to save the configuration file</param>
    public void SaveToFile(string configPath)
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save configuration to {configPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate the configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(SteamWebApiToken))
        {
            Console.WriteLine("Error: Steam Web API token is required");
            return false;
        }

        if (HostSteamId == 0)
        {
            Console.WriteLine("Error: Host Steam ID is required");
            return false;
        }

        if (Port <= 0 || Port > 65535)
        {
            Console.WriteLine("Error: Port must be between 1 and 65535");
            return false;
        }

        if (MaxPlayers <= 0 || MaxPlayers > 10)
        {
            Console.WriteLine("Error: Max players must be between 1 and 10");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Apply command line arguments to override configuration values
    /// </summary>
    /// <param name="args">Command line arguments</param>
    public void ApplyCommandLineArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i += 2)
        {
            var parameter = args[i];
            if (i + 1 >= args.Length) // If param does not have arg after it, skip
                break;

            bool didParse;
            switch (parameter)
            {
                case "--port":
                    didParse = int.TryParse(args[i + 1], out var port);
                    if (didParse && port > 0 && port <= 65535)
                    {
                        Port = port;
                        Console.WriteLine($"Port set to: {Port}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid port '{args[i + 1]}', using default: {Port}");
                    }
                    break;

                case "--steam_web_api_token":
                    SteamWebApiToken = args[i + 1];
                    if (!string.IsNullOrWhiteSpace(SteamWebApiToken))
                    {
                        Console.WriteLine($"Steam Web API token set: {SteamWebApiToken.Truncate(4)}");
                    }
                    else
                    {
                        Console.WriteLine("Warning: Empty Steam Web API token provided");
                    }
                    break;

                case "--host_steamid":
                    didParse = ulong.TryParse(args[i + 1], out var hostSteamId);
                    if (didParse && hostSteamId != 0)
                    {
                        HostSteamId = hostSteamId;
                        Console.WriteLine($"Host Steam ID set to: {HostSteamId}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid host Steam ID '{args[i + 1]}'");
                    }
                    break;

                case "--max_players":
                    didParse = int.TryParse(args[i + 1], out var maxPlayers);
                    if (didParse && maxPlayers > 0 && maxPlayers <= 10)
                    {
                        MaxPlayers = maxPlayers;
                        Console.WriteLine($"Max players set to: {MaxPlayers}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid max players '{args[i + 1]}', using default: {MaxPlayers}");
                    }
                    break;

                case "--auto_start_single_player":
                    didParse = bool.TryParse(args[i + 1], out var autoStart);
                    if (didParse)
                    {
                        AutoStartSinglePlayer = autoStart;
                        Console.WriteLine($"Auto start single player set to: {AutoStartSinglePlayer}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid auto start single player value '{args[i + 1]}', using default: {AutoStartSinglePlayer}");
                    }
                    break;

                case "--auto_start_multiplayer":
                    didParse = bool.TryParse(args[i + 1], out var autoStartMulti);
                    if (didParse)
                    {
                        AutoStartMultiplayer = autoStartMulti;
                        Console.WriteLine($"Auto start multiplayer set to: {AutoStartMultiplayer}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid auto start multiplayer value '{args[i + 1]}', using default: {AutoStartMultiplayer}");
                    }
                    break;

                case "--min_players_for_auto_start":
                    didParse = int.TryParse(args[i + 1], out var minPlayers);
                    if (didParse && minPlayers >= 2 && minPlayers <= 10)
                    {
                        MinPlayersForAutoStart = minPlayers;
                        Console.WriteLine($"Minimum players for auto start set to: {MinPlayersForAutoStart}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid minimum players value '{args[i + 1]}', using default: {MinPlayersForAutoStart}");
                    }
                    break;

                case "--config":
                    // Handle config file loading
                    var configFromFile = LoadFromFile(args[i + 1]);
                    Port = configFromFile.Port;
                    MaxPlayers = configFromFile.MaxPlayers;
                    EnableLogging = configFromFile.EnableLogging;
                    LogPath = configFromFile.LogPath;
                    AuthDelayMs = configFromFile.AuthDelayMs;
                    EnableConsoleOutput = configFromFile.EnableConsoleOutput;
                    GameOptions = configFromFile.GameOptions;
                    AutoStartSinglePlayer = configFromFile.AutoStartSinglePlayer;
                    AutoStartMultiplayer = configFromFile.AutoStartMultiplayer;
                    MinPlayersForAutoStart = configFromFile.MinPlayersForAutoStart;
                    // Don't override credentials from config file for security
                    Console.WriteLine($"Configuration loaded from: {args[i + 1]}");
                    break;

                default:
                    Console.WriteLine($"Unrecognized server argument '{parameter}', ignoring...");
                    break;
            }
        }
    }
}
