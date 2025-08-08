namespace SF_Server;

/// <summary>
/// Handles graceful server shutdown
/// </summary>
public static class ShutdownHandler
{
    private static volatile bool _shutdownRequested = false;
    private static readonly List<Action> _shutdownActions = new();
    
    public static bool ShutdownRequested => _shutdownRequested;
    
    /// <summary>
    /// Initialize the shutdown handler
    /// </summary>
    public static void Initialize()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            Console.WriteLine("\nShutdown requested via Ctrl+C...");
            RequestShutdown();
        };
        
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            Console.WriteLine("Process exit detected...");
            RequestShutdown();
        };
    }
    
    /// <summary>
    /// Request a graceful shutdown
    /// </summary>
    public static void RequestShutdown()
    {
        if (_shutdownRequested) return;
        
        _shutdownRequested = true;
        Console.WriteLine("Initiating graceful shutdown...");
        
        // Execute all registered shutdown actions
        foreach (var action in _shutdownActions)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during shutdown action: {ex.Message}");
            }
        }
        
        Console.WriteLine("Shutdown complete.");
    }
    
    /// <summary>
    /// Register an action to be executed during shutdown
    /// </summary>
    /// <param name="action">Action to execute on shutdown</param>
    public static void RegisterShutdownAction(Action action)
    {
        _shutdownActions.Add(action);
    }
}