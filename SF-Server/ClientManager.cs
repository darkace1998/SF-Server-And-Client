using System.Globalization;
using System.Net;
using Lidgren.Network;

namespace SFServer
{
    /// <summary>
    /// Manages connected clients and their state.
    /// </summary>
    public class ClientManager
    {
        private const string RemovedOldConnectionMessage = "Removed old connection to allow new Steam ID from same IP";
        private const string ServerFullMessage = "Server is full, cannot add new client";
        private const string AddedNewClientFormat = "Added new client at index {0}";
        private const string ClientRemovedFormat = "Client removed at index {0}";
        private readonly ClientInfo[] _clients;
        private readonly Dictionary<IPAddress, DateTime> _connectionAttempts;
        private readonly TimeSpan _connectionCooldown = TimeSpan.FromSeconds(5);

        public ClientInfo[] GetClients() => (ClientInfo[])_clients.Clone();
        public IEnumerable<ClientInfo> AllClients => _clients;
        public ClientInfo[] Clients => _clients;

        public ClientManager(int numClients)
        {
            _clients = new ClientInfo[numClients];
            _connectionAttempts = new Dictionary<IPAddress, DateTime>();
        }

        public bool IsConnectionAllowed(IPAddress address)
        {
            if (!_connectionAttempts.TryGetValue(address, out var lastAttempt))
                return true;
            return DateTime.UtcNow - lastAttempt > _connectionCooldown;
        }

        public void RecordConnectionAttempt(IPAddress address)
        {
            _connectionAttempts[address] = DateTime.UtcNow;
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

        public bool AddNewClient(SteamId steamID, string steamUsername, AuthTicket authTicket, IPAddress address)
        {
            var existingClient = GetClient(steamID);
            if (existingClient != null)
            {
                Console.WriteLine($"Client with Steam ID {steamID} is already connected at index {existingClient.PlayerIndex}");
                existingClient.Address = address;
                existingClient.Status = NetConnectionStatus.Connected;
                Console.WriteLine($"Updated existing client connection: {existingClient}");
                return false;
            }
            var existingIpClient = GetClient(address);
            if (existingIpClient != null)
            {
                Console.WriteLine(RemovedOldConnectionMessage);
                RemoveClient(existingIpClient);
                Console.WriteLine(RemovedOldConnectionMessage);
            }
            var playerIndex = GetEmptyPlayerIndex();
            if (playerIndex == -1)
            {
                Console.WriteLine(ServerFullMessage);
                return false;
            }
            var newClient = new ClientInfo(steamID, steamUsername, authTicket, address, playerIndex);
            _clients[playerIndex] = newClient;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, AddedNewClientFormat, playerIndex));
            Console.WriteLine(newClient.ToString());
            return true;
        }

        public void RemoveClient(ClientInfo removedClient)
        {
            for (var i = 0; i < _clients.Length; i++)
            {
                var client = _clients[i];
                if (client is not null && client.Equals(removedClient))
                {
                    _clients[i] = null;
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ClientRemovedFormat, i));
                    return;
                }
            }
        }

        public void RemoveDisconnectedClients()
        {
            for (var i = 0; i < _clients.Length; i++)
            {
                var client = _clients[i];
                if (client is not null && client.Status == NetConnectionStatus.Disconnected)
                    _clients[i] = null;
            }
        }

        private int GetEmptyPlayerIndex()
        {
            for (var i = 0; i < _clients.Length; i++)
                if (_clients[i] is null)
                    return i;
            return -1;
        }

        public void PostRoundCleanup()
        {
            foreach (var client in _clients)
                client?.Revive();
        }

        public int GetNumLivingClients() => _clients.Count(client => client != null && client.IsAlive);

        public ClientInfo GetClient(IPAddress address)
            => Array.Find(_clients, player => player is not null && Equals(player.Address, address));

        public ClientInfo GetClient(int playerIndex)
            => _clients[playerIndex];

        public ClientInfo GetClient(SteamId id)
            => Array.Find(_clients, player => player is not null && player.SteamID == id);
    }
}
