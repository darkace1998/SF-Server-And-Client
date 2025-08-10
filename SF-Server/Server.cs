using System.Text;
using Lidgren.Network;
using System.Text.Json;

namespace SFServer;

/// <summary>
/// SF-Server main server class handling game server operations
/// </summary>
public class Server : IDisposable
{
    // Security constraints for input validation
    private const int MaxStringLength = 256;
    private const int MaxPacketSize = 1024;

    public string ServerLogPath { get; }

    private readonly NetServer _masterServer;
    private readonly ClientManager _clientMgr;
    private readonly MapManager _mapMgr;
    private readonly ServerConfig _config;
    private readonly string _webApitoken;
    private readonly SteamId _hostSteamId;
    private readonly HttpClient _httpClient;
    private readonly PacketWorker _packetWorker;
    private readonly Random _rand;
    private readonly JsonSerializerOptions _jsonOptions;
    //private readonly List<IPAddress> _approvedIPs;
    private const string LidgrenIdentifier = "monky.SF_Lidgren";
    private const string StickFightAppId = "674940";

    private int NumberOfClients => _masterServer.Connections.Count;

    public Server(ServerConfig config)
    {
        _config = config;

        var netConfig = new NetPeerConfiguration(LidgrenIdentifier)
        {
            Port = config.Port,
            MaximumConnections = config.MaxPlayers
        };

        netConfig.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
        netConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

        var server = new NetServer(netConfig);
        ServerLogPath = Path.Combine(Environment.CurrentDirectory, config.LogPath);
        _masterServer = server;
        _webApitoken = config.SteamWebApiToken;
        _hostSteamId = new SteamId(config.HostSteamId);
        _rand = new Random();

        // Configure HttpClient with security settings
        var httpHandler = new HttpClientHandler()
        {
            CheckCertificateRevocationList = true // Security: Enable certificate revocation checking
        };
        _httpClient = new HttpClient(httpHandler)
        {
            Timeout = TimeSpan.FromSeconds(30) // Security: Add timeout to prevent hanging requests
        };
        _packetWorker = new PacketWorker(this);
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _clientMgr = new ClientManager(config.MaxPlayers);
        _mapMgr = new MapManager(config);
        //_approvedIPs = new List<IPAddress>();
    }

    // Legacy constructor for backward compatibility
    public Server(int port, string steamWebApiToken, SteamId hostSteamId)
        : this(new ServerConfig
        {
            Port = port,
            SteamWebApiToken = steamWebApiToken,
            HostSteamId = hostSteamId.id
        })
    {
    }

    public bool Start()
    {
        _masterServer.Start();

        Console.WriteLine("Starting up UDP socket server: " + _masterServer.Status);
        if (string.IsNullOrEmpty(_webApitoken))
        {
            Console.WriteLine("Invalid steam web api token, please specify it properly as a program parameter. " +
                              "This is required for user auth so the server won't start without it.");
        }

        return _masterServer.Status == NetPeerStatus.Running;
    }

    public void Close()
    {
        _masterServer.Shutdown("Shutting down server.");
        Console.WriteLine("Server has been shutdown.");
    }

    public void Update()
    {
        var msg = _masterServer.ReadMessage();
        if (msg is null) return;

        switch (msg.MessageType)
        {
            case NetIncomingMessageType.VerboseDebugMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.DebugMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.WarningMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.ErrorMessage:
                PrintStatusStr(msg);
                break;
            case NetIncomingMessageType.Error:
                break;
            case NetIncomingMessageType.StatusChanged:
                OnClientStatusChanged(msg);
                break;
            case NetIncomingMessageType.UnconnectedData:
                break;
            case NetIncomingMessageType.ConnectionApproval:
                OnPlayerRequestingConnection(msg);
                return; // Don't want msg being null in async auth method
            case NetIncomingMessageType.Data:
                _packetWorker.ParseGamePacket(msg);
                break;
            case NetIncomingMessageType.Receipt:
                break;
            case NetIncomingMessageType.DiscoveryRequest:
                OnPlayerDiscovered(msg);
                break;
            default:
                Console.WriteLine("Unhandled type: " + msg.MessageType);
                break;
        }

        Console.WriteLine("Recycling msg with length: " + msg.Data.Length + "\n");
        _masterServer.Recycle(msg);
    }

    private void OnPlayerDiscovered(NetIncomingMessage msg)
    {
        var senderEndPoint = msg.SenderEndPoint;
        var response = _masterServer.CreateMessage();
        response.Write("You have discovered Monky's server, greetings!");

        _masterServer.SendDiscoveryResponse(response, senderEndPoint);
        Console.WriteLine("Player discovered, sending response to: " + senderEndPoint.Address);
    }

    private void OnPlayerRequestingConnection(NetIncomingMessage msg)
    {
        var address = msg.GetSenderIP();

        // Check connection cooldown
        if (!_clientMgr.IsConnectionAllowed(address))
        {
            Console.WriteLine($"Connection from {address} denied due to cooldown");
            msg.SenderConnection.Deny("Too many connection attempts. Please wait before trying again.");
            _masterServer.Recycle(msg);
            return;
        }

        // Record this connection attempt
        _clientMgr.RecordConnectionAttempt(address);

        if (NumberOfClients == _config.MaxPlayers)
        {
            Console.WriteLine("Server is full, refusing connection...");
            msg.SenderConnection.Deny("Server is full, try again later.");
            _masterServer.Recycle(msg);
            return;
        }

        Console.WriteLine($"Attempting to authenticate user from {address}...");
        var client = _clientMgr.GetClient(address);

        if (client is not null)
        {
            Console.WriteLine($"Client detected as re-connecting: {client.Username} (Steam ID: {client.SteamID})");
            // Don't remove the client here, let the authentication process handle duplicates
        }

        Console.WriteLine("Starting authentication process...");
        Task.Run(() => AuthenticateUser(msg)); // Client should always auth when joining even if they've joined before
    }

    /// <summary>
    /// Handle client status changes with security validation
    /// </summary>
    /// <param name="msg">Status change message</param>
    private void OnClientStatusChanged(NetIncomingMessage msg)
    {
        // Security: Validate packet size
        if (!ValidatePacketSize(msg))
        {
            LogSecurityEvent("OVERSIZED_PACKET", "Status change packet too large", msg.SenderConnection);
            return;
        }

        try
        {
            var newStatus = (NetConnectionStatus)msg.ReadByte();
            var changeReason = SafeReadString(msg, 256);

            if (changeReason == null)
            {
                LogSecurityEvent("INVALID_STATUS_REASON", "Invalid status change reason", msg.SenderConnection);
                changeReason = "Unknown";
            }

            Console.WriteLine("Client's status changed: " + newStatus + "\nReason: " + changeReason);

            switch (newStatus)
            {
                case NetConnectionStatus.RespondedConnect:
                    Console.WriteLine("Number of clients connected is now: " + NumberOfClients);
                    return;
                case NetConnectionStatus.Disconnected:
                    OnPlayerExit(msg);
                    return;
                case NetConnectionStatus.None:
                case NetConnectionStatus.InitiatedConnect:
                case NetConnectionStatus.ReceivedInitiation:
                case NetConnectionStatus.RespondedAwaitingApproval:
                case NetConnectionStatus.Connected:
                case NetConnectionStatus.Disconnecting:
                default:
                    return;
            }
        }
        catch (Exception ex)
        {
            LogSecurityEvent("STATUS_CHANGE_ERROR", $"Error processing status change: {ex.Message}", msg.SenderConnection);
        }
    }

    private void OnPlayerExit(NetIncomingMessage msg)
    {
        var exitingPlayer = _clientMgr.GetClient(msg.GetSenderIP());
        if (exitingPlayer is null) return;

        Console.WriteLine("Client is leaving: " + exitingPlayer.Username);
        exitingPlayer.Status = NetConnectionStatus.Disconnected;
        _clientMgr.RemoveDisconnectedClients();
    }

    private async Task AuthenticateUser(NetIncomingMessage msg)
    {

        var senderConnection = msg.SenderConnection;

        // Post to Steam Web API to verify ticket
        var authResult = await VerifyAuthTicketRequest(msg).ConfigureAwait(false);

        Console.WriteLine("IS PLAYER AUTHED: " + authResult);
        if (!authResult) // Player is not authed
        {
            Console.WriteLine("Player is not authorized by Steam, denying...");
            msg.SenderConnection.Deny("You are not authorized under Steam."); // Client will not join
            _masterServer.Recycle(msg);
            return;
        }

        Console.WriteLine("Player has successfully authed, allowing them to join...");
        //_approvedIPs.Add(senderConnection.RemoteEndPoint.Address);
        senderConnection.Approve(); // Client will join
        _masterServer.Recycle(msg);
        //_masterServer.SendToAll();
    }

    // Auth via web request
    private async Task<bool> VerifyAuthTicketRequest(NetIncomingMessage msg)
    {
        if (msg.Data is null) return false;

        var authTicket = new AuthTicket(msg.Data);
        Console.WriteLine("Attempting to verify user ticket: " + authTicket);

        var authTicketUri = "https://api.steampowered.com//ISteamUserAuth/AuthenticateUserTicket/v1/" +
                            $"?key={_webApitoken}&appid={StickFightAppId}&ticket={authTicket}&steamid={_hostSteamId}";
        Console.WriteLine("auth ticket uri: " + authTicketUri);

        await Task.Delay(_config.AuthDelayMs).ConfigureAwait(false); // Delay request to reduce false positives of a ticket being invalid
        var jsonResponse = await _httpClient.GetStringAsync(authTicketUri).ConfigureAwait(false);
        Console.WriteLine("Steam auth json response: " + jsonResponse);

        var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse, _jsonOptions);

        if (authResponse?.Response.Params is null) // Client cannot be authed because json was null or "error" was returned
        {
            Console.WriteLine("Auth request returned error, denying connection!!");
            return false;
        }

        var authResponseData = authResponse.Response.Params;

        Console.WriteLine("AuthResponse parsed: " + authResponse);

        if (authResponseData is { Result: not "OK", Publisherbanned: true, Vacbanned: true }) // Client cannot be authed
            return false;

        Console.WriteLine("Auth has not returned error, attempting to parse steamID");

        var playerSteamID = new SteamId(ulong.Parse(authResponseData.Steamid));

        if (playerSteamID.IsBadId()) return false; // Double check validity of steamID

        var playerUsername = await FetchSteamUserName(playerSteamID).ConfigureAwait(false);

        var clientAdded = _clientMgr.AddNewClient(playerSteamID,
            playerUsername,
            authTicket,
            msg.GetSenderIP());

        if (!clientAdded)
        {
            Console.WriteLine("Client was not added (may be duplicate connection), but authentication succeeded");
        }

        return true;
    }

    private async Task<string> FetchSteamUserName(SteamId clientSteamId)
    {
        var playerSummariesUri = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/" +
                                  $"?key={_webApitoken}&steamids={clientSteamId}";

        var jsonResponse = await _httpClient.GetStringAsync(playerSummariesUri).ConfigureAwait(false);
        var profileSummary = JsonSerializer.Deserialize<ProfileSummaryResponse>(jsonResponse, _jsonOptions);

        if (profileSummary is null || profileSummary.Response.Players.Count == 0)
            return "NOT_FOUND";

        return profileSummary.Response.Players[0].Personaname; // The client's steam name
    }

    private static void PrintStatusStr(NetBuffer msg)
        => Console.WriteLine(msg.ReadString());

    // *************************************************
    // Methods to be executed involving packets sent to and from game
    // *************************************************

    // Get current time in milliseconds for packet timing
    private uint GetCurrentTime() => (uint)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & 0xFFFFFFFF);

    public void SendPacketToUser(NetConnection user, byte[] data, SfPacketType messageType,
        NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0)
    {
        var msg = _masterServer.CreateMessage(5 + data.Length); // 5 extra bytes for uint timeSent and byte msgType
        msg.Write(GetCurrentTime()); // Current time instead of uint.MaxValue
        msg.Write((byte)messageType); // Packet type
        msg.Write(data);  // packet data

        _masterServer.SendMessage(msg, user, sendMethod, channel);

        if (_config.EnableLogging)
        {
            Console.WriteLine($"Sent packet to {user.RemoteEndPoint}: Type={messageType}, Size={data.Length}, Method={sendMethod}");
        }
    }

    public void SendPacketToAllUsers(byte[] data, SfPacketType messageType,
        NetConnection ignoredUser = null, NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered,
        int channel = 0)
    {
        var msg = _masterServer.CreateMessage(5 + data.Length); // 5 extra bytes for uint timeSent and byte msgType
        msg.Write(GetCurrentTime()); // Current time instead of uint.MaxValue
        msg.Write((byte)messageType); // Packet type
        msg.Write(data);  // packet data

        _masterServer.SendToAll(msg, ignoredUser, sendMethod, channel);

        if (_config.EnableLogging)
        {
            var connectionCount = _masterServer.Connections.Count - (ignoredUser != null ? 1 : 0);
            Console.WriteLine($"Broadcast packet to {connectionCount} clients: Type={messageType}, Size={data.Length}, Method={sendMethod}");
        }
    }

    public void OnPlayerRequestingIndex(NetConnection user)
    {
        var playerInfo = _clientMgr.GetClient(user.RemoteEndPoint.Address);

        Console.WriteLine("This client's index will be: " + playerInfo.PlayerIndex);
        var tempMsg = _masterServer.CreateMessage();
        tempMsg.Write((byte)playerInfo.PlayerIndex);
        tempMsg.Write(playerInfo.SteamID.id);

        SendPacketToAllUsers(tempMsg.Data, SfPacketType.ClientJoined, user);
        tempMsg = _masterServer.CreateMessage();

        tempMsg.Write((byte)1); // Client accepted as long as this is '1'
        tempMsg.Write((byte)playerInfo.PlayerIndex);

        // Use MapManager for current map info
        var currentMap = _mapMgr.CurrentMapType == MapType.Lobby ?
            _mapMgr.GetLobbyMap() :
            new MapData { MapType = _mapMgr.CurrentMapType, MapId = _mapMgr.CurrentMapId };

        tempMsg.Write((byte)currentMap.MapType); // Map type
        tempMsg.Write(4); // Int representing number of bytes map has, 4 for single int
        tempMsg.Write(currentMap.MapId); // Map data

        foreach (var client in _clientMgr.Clients) // Should only be non-null clients
        {
            // Write steamId of clients or if player index is empty signal this with invalid steamId
            tempMsg.Write(client is not null ? client.SteamID.id : 0UL);

            // Only actual/non-connecting users from here
            if (client is null || Equals(client.Address, user.RemoteEndPoint.Address))
                continue;

            // TODO: Create proper PlayerStats obj
            for (var i = 0; i < 13; i++) tempMsg.Write(0); // Write 0's for 13 empty stats until that's handled
        }

        tempMsg.Write(ushort.MinValue); // TODO: Figure this out properly (something weapons)?

        // TODO: Figure out NetworkOptions (maps, hp, regen, weaponsSpawn in that order)
        tempMsg.Write((byte)0);
        tempMsg.Write((byte)0);
        tempMsg.Write((byte)0);
        tempMsg.Write((byte)0);

        // foreach (var b in tempMsg.Data) 
        //     Console.Write(b);
        // Console.WriteLine(); 

        SendPacketToUser(user, tempMsg.Data, SfPacketType.ClientInit);
    }

    public void OnPlayerRequestingToSpawn(NetConnection user, NetIncomingMessage spawnPosData)
    {
        var tempMsg = _masterServer.CreateMessage();
        tempMsg.Write(spawnPosData.ReadBytes(25)); // Contains player index, spawn pos. vector, and rotation vector

        // Use MapManager to determine spawn position modification
        var shouldModifySpawn = _mapMgr.CurrentMapType != MapType.Lobby && NumberOfClients > 1;
        tempMsg.Write(shouldModifySpawn); // Changes spawn pos if not on lobby map and more than 1 player

        SendPacketToAllUsers(tempMsg.ReadBytes(26), SfPacketType.ClientSpawned);
    }

    public void OnPlayerUpdate(NetConnection user, NetIncomingMessage playerUpdateData)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);

        // foreach (var b in playerUpdateData.Data)
        // {
        //     Console.WriteLine(b);
        // }

        // 10th and 11th bytes should make up ushort representing the # of projectiles
        // var earlyNumProjectilesBytes = new[] { playerUpdateData.Data[9], playerUpdateData.Data[10] };
        // var earlyNumProjectiles = BitConverter.ToUInt16(earlyNumProjectilesBytes);

        // Console.WriteLine("Number of projectiles: " + earlyNumProjectiles);

        // We need to send this packet out to the rest of the clients in the server, +1 byte for weapon type
        SendPacketToAllUsers(
            playerUpdateData.PeekBytes(playerUpdateData.Data.Length - 5),
            SfPacketType.PlayerUpdate,
            user,
            NetDeliveryMethod.UnreliableSequenced,
            playerUpdateData.SequenceChannel
            );

        var positionInfo = new PositionPackage
        {
            Position = new Vector3
            {
                Y = SafeReadInt16(playerUpdateData) / 100f,
                Z = SafeReadInt16(playerUpdateData) / 100f
            },
            Rotation = new Vector2
            {
                X = playerUpdateData.ReadSByte() / 100f,
                Y = playerUpdateData.ReadSByte() / 100f
            },
            YValue = playerUpdateData.ReadSByte(),
            MovementType = playerUpdateData.ReadByte()
        };

        client.PositionInfo = positionInfo;

        var weaponInfo = new WeaponPackage
        {
            FightState = playerUpdateData.ReadByte()
        };

        var numProjectiles = playerUpdateData.ReadUInt16();
        var projectiles = new ProjectilePackage[numProjectiles];

        for (ushort i = 0; i < projectiles.Length; i++)
        {
            projectiles[i] = new ProjectilePackage(new Vector2
            {
                X = SafeReadInt16(playerUpdateData),
                Y = SafeReadInt16(playerUpdateData)
            },
                new Vector2
                {
                    X = playerUpdateData.ReadSByte(),
                    Y = playerUpdateData.ReadSByte()
                },
                playerUpdateData.ReadUInt16());
        }

        weaponInfo.WeaponType = playerUpdateData.ReadByte();
        client.WeaponInfo = weaponInfo;

        //Console.WriteLine("Position info: " + positionInfo);
        //Console.WriteLine("Weapon info: " + weaponInfo);
    }

    public void OnPlayerForceAdded(NetConnection user, NetIncomingMessage damageData)
    {
        // TODO: Add logic for updating anything serverside
        // var client = GetClient(user.RemoteEndPoint.Address);

        SendPacketToAllUsers(
            damageData.PeekBytes(damageData.Data.Length - 5),
            SfPacketType.PlayerForceAdded,
            user,
            NetDeliveryMethod.ReliableOrdered,
            damageData.SequenceChannel
        );
    }

    public void OnPlayerTookDamage(NetConnection user, NetIncomingMessage damageData)
    {
        Console.WriteLine("Sending playertookdamage packet...");

        SendPacketToAllUsers(
            damageData.PeekBytes(damageData.Data.Length - 5),
            SfPacketType.PlayerTookDamage,
            null,
            NetDeliveryMethod.ReliableOrdered,
            damageData.SequenceChannel
        );

        // TODO: Finish logic for updating client's serverside hp
        var attackerClient = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        var damagedClientEventChannel = damageData.SequenceChannel;

        var damagedClient = _clientMgr.GetClient((damagedClientEventChannel - 3) / 2);
        var dmgAmount = SafeReadFloat(damageData);

        // Security: Validate damage amount to prevent exploits
        if (dmgAmount < 0 || dmgAmount > 1000) // Reasonable damage limits
        {
            LogSecurityEvent("INVALID_DAMAGE", $"Invalid damage amount: {dmgAmount}", user);
            return;
        }

        damagedClient.DeductHp(dmgAmount);

        // TODO: Have server send out map packet--Integrate custom orders and custom maps
        //if (damagedClient.IsAlive || _clientMgr.GetNumLivingClients() != 0) return;
        // Round is over, 1 player left living

        //_clientMgr.PostRoundCleanup();

        // Console.WriteLine("Round ended, attempting to change map!");
        // var msg = _masterServer.CreateMessage(); // winner index, maptype, mapdata
        // msg.Write((byte)attackerClient.PlayerIndex);
        // msg.Write(0);
        // msg.Write(_rand.Next(0, 110));
        //
        // SendPacketToAllUsers(
        //     msg.Data,
        //     SfPacketType.MapChange
        // );
    }

    public void OnMapChanged(NetConnection user, NetIncomingMessage mapMsgData)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        var mapData = mapMsgData.PeekBytes(mapMsgData.Data.Length - 5);

        if (!_mapMgr.ValidateMapChange(client?.PlayerIndex ?? -1, mapData))
        {
            Console.WriteLine($"Invalid map change request from {client?.Username ?? "unknown"}");
            return;
        }

        Console.WriteLine($"Map change requested by {client?.Username ?? "unknown"}");
        _mapMgr.ProcessMapChange(mapData);

        SendPacketToAllUsers(
            mapData,
            SfPacketType.MapChange,
            null,
            NetDeliveryMethod.ReliableOrdered,
            mapMsgData.SequenceChannel
        );

        Console.WriteLine($"Map changed to ID: {_mapMgr.CurrentMapId}, Type: {_mapMgr.CurrentMapType}");
    }

    /// <summary>
    /// Handle player chat messages with security validation
    /// </summary>
    /// <param name="user">Connection from user</param>
    /// <param name="chatMsgData">Chat message data</param>
    public void OnPlayerTalked(NetConnection user, NetIncomingMessage chatMsgData)
    {
        // Security: Validate packet size
        if (!ValidatePacketSize(chatMsgData))
        {
            LogSecurityEvent("OVERSIZED_PACKET", "Chat message packet too large", user);
            return;
        }

        var chatMsgBytes = chatMsgData.PeekBytes(chatMsgData.Data.Length - 5);
        var chatMsg = Encoding.UTF8.GetString(chatMsgBytes);

        // Security: Validate chat message content
        if (!ValidateStringInput(chatMsg, 512)) // Allow longer chat messages
        {
            LogSecurityEvent("INVALID_CHAT", "Invalid chat message format", user);
            return;
        }

        var sender = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        if (sender == null)
        {
            LogSecurityEvent("UNKNOWN_SENDER", "Chat from unregistered client", user);
            return;
        }

        Console.WriteLine($"{sender.Username}: {chatMsg}");

        SendPacketToAllUsers(
            chatMsgBytes,
            SfPacketType.PlayerTalked,
            user, // Don't need to transmit the msg to the original sender
            NetDeliveryMethod.ReliableOrdered,
            chatMsgData.SequenceChannel
        );
    }

    /// <summary>
    /// Security: Validate string input from network messages
    /// </summary>
    /// <param name="input">Input string to validate</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool ValidateStringInput(string input, int maxLength = MaxStringLength)
    {
        return !string.IsNullOrEmpty(input) && input.Length <= maxLength && !input.Contains('\0');
    }

    /// <summary>
    /// Security: Validate packet size
    /// </summary>
    /// <param name="message">Network message to validate</param>
    /// <returns>True if valid size, false otherwise</returns>
    private static bool ValidatePacketSize(NetIncomingMessage message)
    {
        return message.LengthBytes <= MaxPacketSize;
    }

    /// <summary>
    /// Security: Safe string reading with validation
    /// </summary>
    /// <param name="message">Network message</param>
    /// <param name="maxLength">Maximum string length</param>
    /// <returns>Validated string or null if invalid</returns>
    private static string SafeReadString(NetIncomingMessage message, int maxLength = MaxStringLength)
    {
        try
        {
            var str = message.ReadString();
            return ValidateStringInput(str, maxLength) ? str : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Security: Safe float reading with validation
    /// </summary>
    /// <param name="message">Network message</param>
    /// <returns>Float value or 0 if invalid</returns>
    private static float SafeReadFloat(NetIncomingMessage message)
    {
        try
        {
            var value = message.ReadFloat();
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
        catch (Exception)
        {
            return 0f;
        }
    }

    /// <summary>
    /// Security: Safe int16 reading with validation
    /// </summary>
    /// <param name="message">Network message</param>
    /// <returns>Int16 value or 0 if invalid</returns>
    private static short SafeReadInt16(NetIncomingMessage message)
    {
        try
        {
            return message.ReadInt16();
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Security: Log security events
    /// </summary>
    /// <param name="eventType">Type of security event</param>
    /// <param name="details">Event details</param>
    /// <param name="connection">Associated connection if any</param>
    private void LogSecurityEvent(string eventType, string details, NetConnection connection = null)
    {
        var logMessage = $"[SECURITY] {eventType}: {details}";
        if (connection != null)
        {
            logMessage += $" from {connection.RemoteEndPoint}";
        }
        Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {logMessage}");
    }

    /// <summary>
    /// Dispose pattern implementation for proper resource cleanup
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
            _masterServer?.Shutdown("Server shutting down");
        }
    }
}
