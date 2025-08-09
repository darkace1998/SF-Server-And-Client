using SF_Server;

const string defaultConfigPath = "server_config.json";

Console.WriteLine("SF-Server starting...");
Console.WriteLine($".NET Version: {Environment.Version}");
Console.WriteLine($"OS: {Environment.OSVersion}");

// Initialize graceful shutdown handling
ShutdownHandler.Initialize();

// Load configuration
var config = new ServerConfig();

// Check if a config file was specified as first argument
if (args.Length > 0 && !args[0].StartsWith("--"))
{
    Console.WriteLine($"Loading configuration from: {args[0]}");
    config = ServerConfig.LoadFromFile(args[0]);
    // Skip the config file argument when parsing other args
    args = args.Skip(1).ToArray();
}
else if (File.Exists(defaultConfigPath))
{
    Console.WriteLine($"Loading default configuration from: {defaultConfigPath}");
    config = ServerConfig.LoadFromFile(defaultConfigPath);
}

// Apply command line argument overrides
config.ApplyCommandLineArgs(args);

// Validate configuration
if (!config.IsValid())
{
    Console.WriteLine("\nUsage:");
    Console.WriteLine("  SF-Server [config_file] [options]");
    Console.WriteLine("\nOptions:");
    Console.WriteLine("  --port <port>                    Server port (default: 1337)");
    Console.WriteLine("  --steam_web_api_token <token>    Steam Web API token (required)");
    Console.WriteLine("  --host_steamid <steamid>         Host Steam ID (required)");
    Console.WriteLine("  --max_players <count>            Maximum players (default: 4)");
    Console.WriteLine("  --config <file>                  Load configuration from file");
    Console.WriteLine("\nExample:");
    Console.WriteLine("  SF-Server --port 1337 --steam_web_api_token YOUR_TOKEN --host_steamid 76561198000000000");
    Environment.Exit(1);
}

Console.WriteLine($"\nStarting server with configuration:");
Console.WriteLine($"  Port: {config.Port}");
Console.WriteLine($"  Max Players: {config.MaxPlayers}");
Console.WriteLine($"  Host Steam ID: {config.HostSteamId}");
Console.WriteLine($"  Logging: {(config.EnableLogging ? "Enabled" : "Disabled")}");

// Create and start server
var server = new Server(config);
var serverStarted = server.Start();

if (!serverStarted)
{
    Console.WriteLine($"Server failed to start on port: {config.Port}");
    Environment.Exit(1);
}

// Register shutdown action
ShutdownHandler.RegisterShutdownAction(() =>
{
    Console.WriteLine("Shutting down server...");
    server.Close();
    server.Dispose(); // Security: Proper resource disposal
});

Console.WriteLine("Server started successfully! Press Ctrl+C to shutdown gracefully.");

// Main server loop
while (!ShutdownHandler.ShutdownRequested)
{
    try
    {
        server.Update();
        Thread.Sleep(1); // Small delay to prevent excessive CPU usage
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in server update loop: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }

        // Log stack trace for debugging
        if (config.EnableLogging)
        {
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

Console.WriteLine("Server shutdown completed.");
Environment.Exit(0);
