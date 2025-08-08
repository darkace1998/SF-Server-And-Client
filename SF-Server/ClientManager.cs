using System.Net;
using Lidgren.Network;

namespace SF_Server;

public class ClientManager
{
    public ClientInfo[] Clients { get; }
    private readonly Dictionary<IPAddress, DateTime> _connectionAttempts;
    private readonly TimeSpan _connectionCooldown = TimeSpan.FromSeconds(5);
    
    public ClientManager(int numClients) 
    {
        Clients = new ClientInfo[numClients];
        _connectionAttempts = new Dictionary<IPAddress, DateTime>();
    }
    
    /// <summary>
    /// Check if a connection attempt is allowed from this IP
    /// </summary>
    /// <param name="address">IP address to check</param>
    /// <returns>True if connection is allowed</returns>
    public bool IsConnectionAllowed(IPAddress address)
    {
        if (!_connectionAttempts.TryGetValue(address, out var lastAttempt))
            return true;
            
        return DateTime.UtcNow - lastAttempt > _connectionCooldown;
    }
    
    /// <summary>
    /// Record a connection attempt from an IP address
    /// </summary>
    /// <param name="address">IP address</param>
    public void RecordConnectionAttempt(IPAddress address)
    {
        _connectionAttempts[address] = DateTime.UtcNow;
        
        // Clean up old connection attempts (older than 1 hour)
        var expiredTime = DateTime.UtcNow - TimeSpan.FromHours(1);
        var expiredKeys = _connectionAttempts
            .Where(kvp => kvp.Value < expiredTime)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in expiredKeys)
        {
            _connectionAttempts.Remove(key);
        }
    }

    /// <summary>
    /// Add a new client, preventing duplicate connections
    /// </summary>
    /// <param name="steamID">Steam ID of the client</param>
    /// <param name="steamUsername">Steam username</param>
    /// <param name="authTicket">Authentication ticket</param>
    /// <param name="address">IP address</param>
    /// <returns>True if client was added successfully</returns>
    public bool AddNewClient(SteamId steamID, string steamUsername, AuthTicket authTicket, IPAddress address)
    {
        // Check for existing client with same Steam ID
        var existingClient = GetClient(steamID);
        if (existingClient != null)
        {
            Console.WriteLine($"Client with Steam ID {steamID} is already connected at index {existingClient.PlayerIndex}");
            
            // Update the address in case of IP change (reconnection)
            existingClient.Address = address;
            existingClient.Status = NetConnectionStatus.Connected;
            Console.WriteLine($"Updated existing client connection: {existingClient}");
            return false; // Don't add a new client, but connection is valid
        }
        
        // Check for existing client with same IP address
        var existingIpClient = GetClient(address);
        if (existingIpClient != null)
        {
            Console.WriteLine($"Client with IP {address} is already connected with Steam ID {existingIpClient.SteamID}");
            
            // Remove the old connection to allow the new one
            RemoveClient(existingIpClient);
            Console.WriteLine("Removed old connection to allow new Steam ID from same IP");
        }
        
        var playerIndex = GetEmptyPlayerIndex();
        if (playerIndex == -1)
        {
            Console.WriteLine("Server is full, cannot add new client");
            return false;
        }
        
        var newClient = new ClientInfo(steamID, steamUsername, authTicket, address, playerIndex);
        Clients[playerIndex] = newClient;
        
        Console.WriteLine($"Added new client at index {playerIndex}!");
        Console.WriteLine(newClient.ToString());
        return true;
    }
    
    public void RemoveClient(ClientInfo removedClient)
    {
        for (var i = 0; i < Clients.Length; i++)
        {
            var client = Clients[i];
            
            if (client is not null && client.Equals(removedClient))
            {
                Clients[i] = null; // Frees up spot for new player
                Console.WriteLine("Client removed at index: " + i);
                return;
            }
        }
    }

    public void RemoveDisconnectedClients()
    {
        for (var i = 0; i < Clients.Length; i++)
        {
            var client = Clients[i];
            
            if (client is not null && client.Status == NetConnectionStatus.Disconnected)
                Clients[i] = null; // Frees up spot for new player
        }
    }
    
    private int GetEmptyPlayerIndex()
    {
        for (var i = 0; i < Clients.Length; i++)
            if (Clients[i] is null)
                return i;

        return -1;
    }

    public void PostRoundCleanup()
    {
        foreach (var client in Clients) 
            client.Revive();
    }

    public int GetNumLivingClients() => Clients.Count(client => client.IsAlive);

    public ClientInfo GetClient(IPAddress address) 
        => Clients.FirstOrDefault(player => player is not null && Equals(player.Address, address));

    public ClientInfo GetClient(int playerIndex)
        => Clients[playerIndex];

    public ClientInfo GetClient(SteamId id)
        => Clients.FirstOrDefault(player => player is not null && player.SteamID == id);
}