using System.Text;
using Lidgren.Network;
using System.Text.Json;
using System.Collections.Generic;

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
    public ServerConfig Config => _config;

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
    private uint _lastProcessedTimestamp;
    private DateTime _lastPingTime;
    private const int PingIntervalSeconds = 5; // Send ping every 5 seconds
    private readonly object _logLock = new object();
    private FileStream? _logFileStream;
    private StreamWriter? _logWriter;
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
        
        // Initialize file logging if enabled
        if (config.EnableLogging)
        {
            InitializeFileLogging();
        }
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
        _lastProcessedTimestamp = 0;
        _lastPingTime = DateTime.UtcNow;
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

    /// <summary>
    /// Initialize file logging system
    /// </summary>
    private void InitializeFileLogging()
    {
        try
        {
            // Ensure directory exists
            var logDirectory = Path.GetDirectoryName(ServerLogPath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Create or append to log file
            _logFileStream = new FileStream(ServerLogPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _logWriter = new StreamWriter(_logFileStream, Encoding.UTF8)
            {
                AutoFlush = true // Ensure immediate writing
            };

            // Write session start marker
            LogToFile($"=== SF-Server session started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===");
            LogToFile($"Server configuration: Port={_config.Port}, MaxPlayers={_config.MaxPlayers}, Host={_config.HostSteamId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize file logging: {ex.Message}");
        }
    }

    /// <summary>
    /// Log message to both console and file (if logging enabled)
    /// </summary>
    /// <param name="message">Message to log</param>
    private void Log(string message)
    {
        // Always log to console if console output is enabled
        if (_config.EnableConsoleOutput)
        {
            Console.WriteLine(message);
        }

        // Log to file if logging is enabled
        if (_config.EnableLogging)
        {
            LogToFile(message);
        }
    }

    /// <summary>
    /// Log message directly to file with timestamp
    /// </summary>
    /// <param name="message">Message to log</param>
    private void LogToFile(string message)
    {
        if (_logWriter == null) return;

        lock (_logLock)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                _logWriter.WriteLine($"[{timestamp}] {message}");
            }
            catch (Exception ex)
            {
                // If file logging fails, fallback to console
                Console.WriteLine($"File logging error: {ex.Message}");
                Console.WriteLine($"Original message: {message}");
            }
        }
    }

    /// <summary>
    /// Log debug message (only if debug logging enabled)
    /// </summary>
    /// <param name="message">Debug message to log</param>
    private void LogDebug(string message)
    {
        if (_config.EnableDebugPacketLogging)
        {
            Log($"[DEBUG] {message}");
        }
    }

    public bool Start()
    {
        _masterServer.Start();

        Log("Starting up UDP socket server: " + _masterServer.Status);
        if (string.IsNullOrEmpty(_webApitoken))
        {
            Log("Invalid steam web api token, please specify it properly as a program parameter. " +
                              "This is required for user auth so the server won't start without it.");
        }

        return _masterServer.Status == NetPeerStatus.Running;
    }

    public void Close()
    {
        _masterServer.Shutdown("Shutting down server.");
        Log("Server has been shutdown.");
    }

    public void Update()
    {
        // Send periodic pings to all clients
        if ((DateTime.UtcNow - _lastPingTime).TotalSeconds >= PingIntervalSeconds)
        {
            SendPingToAllClients();
            _lastPingTime = DateTime.UtcNow;
        }

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
                Log("Unhandled type: " + msg.MessageType);
                break;
        }

        LogDebug("Recycling msg with length: " + msg.Data.Length);
        _masterServer.Recycle(msg);
    }

    private void OnPlayerDiscovered(NetIncomingMessage msg)
    {
        var senderEndPoint = msg.SenderEndPoint;
        var response = _masterServer.CreateMessage();
        response.Write("You have discovered Monky's server, greetings!");

        _masterServer.SendDiscoveryResponse(response, senderEndPoint);
        Log("Player discovered, sending response to: " + senderEndPoint.Address);
    }

    private void OnPlayerRequestingConnection(NetIncomingMessage msg)
    {
        var address = msg.GetSenderIP();

        // Check connection cooldown
        if (!_clientMgr.IsConnectionAllowed(address))
        {
            Log($"Connection from {address} denied due to cooldown");
            msg.SenderConnection.Deny("Too many connection attempts. Please wait before trying again.");
            _masterServer.Recycle(msg);
            return;
        }

        // Record this connection attempt
        _clientMgr.RecordConnectionAttempt(address);

        if (NumberOfClients == _config.MaxPlayers)
        {
            Log("Server is full, refusing connection...");
            msg.SenderConnection.Deny("Server is full, try again later.");
            _masterServer.Recycle(msg);
            return;
        }

        Log($"Attempting to authenticate user from {address}...");
        var client = _clientMgr.GetClient(address);

        if (client is not null)
        {
            Log($"Client detected as re-connecting: {client.Username} (Steam ID: {client.SteamID})");
            // Don't remove the client here, let the authentication process handle duplicates
        }

        Log("Starting authentication process...");
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
                    Log("Number of clients connected is now: " + NumberOfClients);
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

        Log("Client is leaving: " + exitingPlayer.Username);
        exitingPlayer.Status = NetConnectionStatus.Disconnected;
        _clientMgr.RemoveDisconnectedClients();
    }

    private async Task AuthenticateUser(NetIncomingMessage msg)
    {

        var senderConnection = msg.SenderConnection;

        // Post to Steam Web API to verify ticket
        var authResult = await VerifyAuthTicketRequest(msg).ConfigureAwait(false);

        Log("IS PLAYER AUTHED: " + authResult);
        if (!authResult) // Player is not authed
        {
            Log("Player is not authorized by Steam, denying...");
            msg.SenderConnection.Deny("You are not authorized under Steam."); // Client will not join
            _masterServer.Recycle(msg);
            return;
        }

        Log("Player has successfully authed, allowing them to join...");
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
        Log("Attempting to verify user ticket: " + authTicket);

        // Development/testing bypass for dummy tokens
        if (_webApitoken == "DUMMY_TOKEN" || _webApitoken == "DEBUG_TOKEN" || _webApitoken.StartsWith("DEVELOPMENT"))
        {
            Log("WARNING: Using dummy Steam Web API token - authentication bypassed for testing");
            Log("This would normally fail in production. Please use a real Steam Web API token.");
            
            // For testing, we can either:
            // 1. Always deny (strict security) - uncomment next line
            // return false;
            
            // 2. Allow for testing purposes (current behavior) - create a fake successful auth
            // This allows testing the connection flow without Steam API
            Log("DEVELOPMENT MODE: Allowing connection for testing purposes");
            
            // Create a fake client for testing
            var testSteamId = new SteamId(76561198000000000UL + (ulong)_rand.Next(1000, 9999));
            var testUsername = $"TestUser_{_rand.Next(1000, 9999)}";
            
            var clientAdded = _clientMgr.AddNewClient(testSteamId, testUsername, authTicket, msg.GetSenderIP());
            
            if (clientAdded)
            {
                Log($"Test client added: {testUsername} (Steam ID: {testSteamId})");
            }
            else
            {
                Log("Failed to add test client (may be duplicate)");
            }
            
            return true; // Allow connection for testing
        }

        var authTicketUri = "https://api.steampowered.com//ISteamUserAuth/AuthenticateUserTicket/v1/" +
                            $"?key={_webApitoken}&appid={StickFightAppId}&ticket={authTicket}&steamid={_hostSteamId}";
        LogDebug("auth ticket uri: " + authTicketUri);

        try
        {
            await Task.Delay(_config.AuthDelayMs).ConfigureAwait(false); // Delay request to reduce false positives of a ticket being invalid
            
            var jsonResponse = await _httpClient.GetStringAsync(authTicketUri).ConfigureAwait(false);
            LogDebug("Steam auth json response: " + jsonResponse);

            var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse, _jsonOptions);

            if (authResponse?.Response.Params is null) // Client cannot be authed because json was null or "error" was returned
            {
                Log("Auth request returned error, denying connection!!");
                return false;
            }

            var authResponseData = authResponse.Response.Params;

            LogDebug("AuthResponse parsed: " + authResponse);

            if (authResponseData is { Result: not "OK", Publisherbanned: true, Vacbanned: true }) // Client cannot be authed
                return false;

            Log("Auth has not returned error, attempting to parse steamID");

            var playerSteamID = new SteamId(ulong.Parse(authResponseData.Steamid));

            if (playerSteamID.IsBadId()) return false; // Double check validity of steamID

            var playerUsername = await FetchSteamUserName(playerSteamID).ConfigureAwait(false);

            var clientAdded = _clientMgr.AddNewClient(playerSteamID,
                playerUsername,
                authTicket,
                msg.GetSenderIP());

            if (!clientAdded)
            {
                Log("Client was not added (may be duplicate connection), but authentication succeeded");
            }

            return true;
        }
        catch (HttpRequestException httpEx)
        {
            Log($"Steam Web API request failed: {httpEx.Message}");
            Log("This usually means the Steam Web API token is invalid or Steam services are unavailable");
            return false;
        }
        catch (TaskCanceledException timeoutEx)
        {
            Log($"Steam Web API request timed out: {timeoutEx.Message}");
            Log("Steam services may be slow or unavailable");
            return false;
        }
        catch (JsonException jsonEx)
        {
            Log($"Failed to parse Steam Web API response: {jsonEx.Message}");
            Log("The response from Steam may be malformed");
            return false;
        }
        catch (Exception ex)
        {
            Log($"Unexpected error during authentication: {ex.Message}");
            LogDebug($"Stack trace: {ex.StackTrace}");
            return false;
        }
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

    private void PrintStatusStr(NetBuffer msg)
        => LogDebug(msg.ReadString());

    // *************************************************
    // Methods to be executed involving packets sent to and from game
    // *************************************************

    /// <summary>
    /// Get current time in milliseconds for packet timing
    /// </summary>
    private static uint GetCurrentTime() => (uint)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & uint.MaxValue);

    /// <summary>
    /// Checks if a packet timestamp is obsolete based on the last processed timestamp
    /// </summary>
    /// <param name="packetTimestamp">The timestamp from the incoming packet</param>
    /// <returns>True if the packet is obsolete and should be discarded</returns>
    public bool IsPacketObsolete(uint packetTimestamp)
    {
        // Allow some tolerance for network jitter (500ms)
        const uint toleranceMs = 500;
        
        // Handle timestamp overflow (uint wraps around)
        if (_lastProcessedTimestamp > uint.MaxValue - toleranceMs && packetTimestamp < toleranceMs)
        {
            // Timestamp has wrapped around, accept it
            _lastProcessedTimestamp = packetTimestamp;
            return false;
        }
        
        // Normal case: check if packet is older than last processed
        if (packetTimestamp < _lastProcessedTimestamp - toleranceMs)
        {
            LogDebug($"Discarding obsolete packet: timestamp={packetTimestamp}, last={_lastProcessedTimestamp}");
            return true;
        }
        
        // Update last processed timestamp
        _lastProcessedTimestamp = Math.Max(_lastProcessedTimestamp, packetTimestamp);
        return false;
    }

    public void SendPacketToUser(NetConnection user, byte[] data, SfPacketType messageType,
        NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0)
    {
        var msg = _masterServer.CreateMessage(5 + data.Length); // 5 extra bytes for uint timeSent and byte msgType
        msg.Write(GetCurrentTime()); // Current time instead of uint.MaxValue
        msg.Write((byte)messageType); // Packet type
        msg.Write(data);  // packet data

        _masterServer.SendMessage(msg, user, sendMethod, channel);

        LogDebug($"Sent packet to {user.RemoteEndPoint}: Type={messageType}, Size={data.Length}, Method={sendMethod}");
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

        var connectionCount = _masterServer.Connections.Count - (ignoredUser != null ? 1 : 0);
        LogDebug($"Broadcast packet to {connectionCount} clients: Type={messageType}, Size={data.Length}, Method={sendMethod}");
    }

    public void OnPlayerRequestingIndex(NetConnection user)
    {
        var playerInfo = _clientMgr.GetClient(user.RemoteEndPoint.Address);

        Log("This client's index will be: " + playerInfo.PlayerIndex);
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
            new MapData(_mapMgr.CurrentMapType, _mapMgr.CurrentMapId, BitConverter.GetBytes(_mapMgr.CurrentMapId));

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

            // Write proper PlayerStats data instead of zeros
            var statsBytes = client.Stats.ToByteArray();
            tempMsg.Write(statsBytes);
        }

        tempMsg.Write(ushort.MinValue); // Default weapon value - consider implementing proper weapon tracking

        // Write proper NetworkOptions instead of hardcoded zeros
        var gameOptionsBytes = _config.GameOptions.ToByteArray();
        tempMsg.Write(gameOptionsBytes);

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
        if (client == null)
        {
            LogSecurityEvent("UNKNOWN_CLIENT", "Player update from unregistered client", user);
            return;
        }

        // Read the position data from the packet
        var newPosition = new Vector3(
            0f, // X value - not provided in this update
            SafeReadInt16(playerUpdateData) / 100f,
            SafeReadInt16(playerUpdateData) / 100f
        );
        
        var newRotation = new Vector2(
            playerUpdateData.ReadSByte() / 100f,
            playerUpdateData.ReadSByte() / 100f
        );
        
        var yValue = playerUpdateData.ReadSByte();
        var movementType = playerUpdateData.ReadByte();

        // Server-side movement validation
        var previousPosition = client.PositionInfo.Position;
        if (!ValidateMovement(previousPosition, newPosition, newRotation, movementType, client))
        {
            LogDebug($"Invalid movement from {client.Username} - rejecting update");
            return; // Reject the movement update
        }

        var positionInfo = new PositionPackage(newPosition, newRotation, yValue, movementType);
        client.PositionInfo = positionInfo;

        var fightState = playerUpdateData.ReadByte();

        var numProjectiles = playerUpdateData.ReadUInt16();
        
        // Validate projectile count to prevent spam/DoS
        if (numProjectiles > 50) // Reasonable max projectiles per update
        {
            LogSecurityEvent("EXCESSIVE_PROJECTILES", $"Too many projectiles: {numProjectiles} from {client.Username}", user);
            return;
        }
        
        var projectiles = new ProjectilePackage[numProjectiles];
        var validProjectiles = new List<ProjectilePackage>();

        for (ushort i = 0; i < numProjectiles; i++)
        {
            var projectile = new ProjectilePackage(new Vector2(
                SafeReadInt16(playerUpdateData),
                SafeReadInt16(playerUpdateData)
            ),
                new Vector2(
                    playerUpdateData.ReadSByte(),
                    playerUpdateData.ReadSByte()
                ),
                playerUpdateData.ReadUInt16());
                
            // Validate each projectile
            if (ValidateProjectile(projectile, client))
            {
                validProjectiles.Add(projectile);
            }
        }

        // Use only validated projectiles
        projectiles = validProjectiles.ToArray();

        var weaponType = playerUpdateData.ReadByte();
        var weaponInfo = new WeaponPackage(weaponType, fightState, projectiles);
        client.WeaponInfo = weaponInfo;

        // Only send the validated update to other clients
        SendPacketToAllUsers(
            RebuildPlayerUpdatePacket(positionInfo, fightState, weaponInfo),
            SfPacketType.PlayerUpdate,
            user,
            NetDeliveryMethod.UnreliableSequenced,
            playerUpdateData.SequenceChannel
        );

        LogDebug($"Validated movement for {client.Username}: {positionInfo}");
    }

    public void OnPlayerForceAdded(NetConnection user, NetIncomingMessage damageData)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        if (client == null)
        {
            LogSecurityEvent("UNKNOWN_CLIENT", "Force packet from unregistered client", user);
            return;
        }

        // Validate force data to prevent physics exploits
        var originalData = damageData.PeekBytes(damageData.Data.Length - 5);
        if (!ValidatePhysicsForce(originalData, client))
        {
            LogSecurityEvent("INVALID_FORCE", $"Invalid physics force from {client.Username}", user);
            return;
        }

        if (_config.EnableLogging)
        {
            Log($"Force applied to player {client.Username}");
        }

        SendPacketToAllUsers(
            originalData,
            SfPacketType.PlayerForceAdded,
            user,
            NetDeliveryMethod.ReliableOrdered,
            damageData.SequenceChannel
        );
    }

    public void OnPlayerTookDamage(NetConnection user, NetIncomingMessage damageData)
    {
        var attackerClient = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        if (attackerClient == null)
        {
            LogSecurityEvent("UNKNOWN_ATTACKER", "Damage packet from unregistered client", user);
            return;
        }

        var damagedClientEventChannel = damageData.SequenceChannel;
        var damagedClient = _clientMgr.GetClient((damagedClientEventChannel - 3) / 2);
        
        if (damagedClient == null)
        {
            LogSecurityEvent("INVALID_TARGET", $"Damage to invalid target from {attackerClient.Username}", user);
            return;
        }

        // Read damage data from client (but don't trust the damage amount)
        var clientDamageAmount = SafeReadFloat(damageData);
        
        // Server-side damage calculation based on weapon type, distance, and game rules
        var calculatedDamage = CalculateServerSideDamage(attackerClient, damagedClient, clientDamageAmount);
        
        // Validate damage is reasonable
        if (calculatedDamage < 0 || calculatedDamage > 100) // Max damage per hit
        {
            LogSecurityEvent("INVALID_CALCULATED_DAMAGE", 
                $"Invalid calculated damage: {calculatedDamage} from {attackerClient.Username} to {damagedClient.Username}", user);
            return;
        }

        // Apply server-calculated damage
        damagedClient.DeductHp(calculatedDamage);

        if (_config.EnableLogging)
        {
            Log($"Player {damagedClient.Username} took {calculatedDamage:F1} damage (client sent {clientDamageAmount:F1}), HP: {damagedClient.Hp:F1}, Alive: {damagedClient.IsAlive}");
        }

        // Create corrected damage packet with server-calculated damage
        var correctedDamagePacket = CreateDamagePacket(damagedClient.PlayerIndex, calculatedDamage);
        
        SendPacketToAllUsers(
            correctedDamagePacket,
            SfPacketType.PlayerTookDamage,
            null,
            NetDeliveryMethod.ReliableOrdered,
            damageData.SequenceChannel
        );

        // Check if player died
        if (!damagedClient.IsAlive)
        {
            Log($"Player {damagedClient.Username} has been killed!");
            
            // Update attacker stats if available
            if (attackerClient != damagedClient)
            {
                Log($"Kill credited to {attackerClient.Username}");
            }
        }

        // Check for round end conditions and handle map changes
        var alivePlayers = _clientMgr.GetNumLivingClients();
        if (alivePlayers <= 1 && _clientMgr.Clients.Count(c => c != null) > 1)
        {
            Log($"Round ending - {alivePlayers} players remaining");
            
            // Implement server-sent map packet for round transitions
            if (alivePlayers == 0)
            {
                // No survivors - send random map change
                var msg = _masterServer.CreateMessage();
                msg.Write(byte.MaxValue); // No winner
                msg.Write((byte)0); // Default map type (lobby)
                msg.Write(_rand.Next(0, 110)); // Random map ID
                
                SendPacketToAllUsers(
                    msg.Data,
                    SfPacketType.MapChange,
                    null,
                    NetDeliveryMethod.ReliableOrdered
                );
                
                Log("Sent map change packet - no survivors");
            }
            else if (attackerClient != null)
            {
                // Winner exists - send map change with winner info
                var msg = _masterServer.CreateMessage();
                msg.Write((byte)attackerClient.PlayerIndex); // Winner index
                msg.Write((byte)0); // Default map type
                msg.Write(_rand.Next(0, 110)); // Random map ID
                
                SendPacketToAllUsers(
                    msg.Data,
                    SfPacketType.MapChange,
                    null,
                    NetDeliveryMethod.ReliableOrdered
                );
                
                Log($"Sent map change packet - winner: {attackerClient.Username}");
            }
            
            // Clean up round state
            _clientMgr.PostRoundCleanup();
        }
    }

    public void OnMapChanged(NetConnection user, NetIncomingMessage mapMsgData)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        var mapData = mapMsgData.PeekBytes(mapMsgData.Data.Length - 5);

        if (!MapManager.ValidateMapChange(client?.PlayerIndex ?? -1, mapData))
        {
            Log($"Invalid map change request from {client?.Username ?? "unknown"}");
            return;
        }

        Log($"Map change requested by {client?.Username ?? "unknown"}");
        _mapMgr.ProcessMapChange(mapData);

        SendPacketToAllUsers(
            mapData,
            SfPacketType.MapChange,
            null,
            NetDeliveryMethod.ReliableOrdered,
            mapMsgData.SequenceChannel
        );

        Log($"Map changed to ID: {_mapMgr.CurrentMapId}, Type: {_mapMgr.CurrentMapType}");
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

        Log($"{sender.Username}: {chatMsg}");

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
    /// Handle ping requests from clients and respond with pong
    /// </summary>
    /// <param name="user">The client connection</param>
    /// <param name="pingData">The ping message data</param>
    public void OnPingReceived(NetConnection user, NetIncomingMessage pingData)
    {
        if (pingData?.Data == null || pingData.Data.Length < 5)
        {
            LogDebug("Received invalid ping packet");
            return;
        }

        try
        {
            // Read the original timestamp from the ping packet (skipping the packet header)
            pingData.Position = 20; // Skip past packet timestamp (4 bytes) and type (1 byte) from ParseGamePacket
            var originalTimestamp = pingData.ReadUInt32();

            // Send ping response with the original timestamp
            var responseData = BitConverter.GetBytes(originalTimestamp);
            SendPacketToUser(user, responseData, SfPacketType.PingResponse, NetDeliveryMethod.ReliableOrdered);

            if (_config.EnableLogging)
            {
                var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
                Log($"Responded to ping from {client?.Username ?? "unknown"} (timestamp: {originalTimestamp})");
            }
        }
        catch (Exception ex)
        {
            LogSecurityEvent("PING_ERROR", $"Error processing ping: {ex.Message}", user);
        }
    }

    /// <summary>
    /// Handle ping responses from clients and calculate round-trip time
    /// </summary>
    /// <param name="user">The client connection</param>
    /// <param name="pongData">The pong message data</param>
    public void OnPingResponseReceived(NetConnection user, NetIncomingMessage pongData)
    {
        if (pongData?.Data == null || pongData.Data.Length < 9) // 5 bytes header + 4 bytes timestamp
        {
            LogDebug("Received invalid ping response packet");
            return;
        }

        try
        {
            var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
            if (client == null)
            {
                LogDebug("Received ping response from unknown client");
                return;
            }

            // Read the original timestamp from the response
            pongData.Position = 20; // Skip packet header
            var originalTimestamp = pongData.ReadUInt32();
            var currentTime = GetCurrentTime();

            // Calculate round-trip time
            var roundTripTime = currentTime - originalTimestamp;
            
            // Handle timestamp overflow
            if (originalTimestamp > currentTime)
            {
                // Timestamp wrapped around
                roundTripTime = (uint.MaxValue - originalTimestamp) + currentTime;
            }

            // Update client ping (limit to reasonable values)
            client.Ping = Math.Min((int)roundTripTime, 9999);

            if (_config.EnableLogging)
            {
                Log($"Updated ping for {client.Username}: {client.Ping}ms");
            }
        }
        catch (Exception ex)
        {
            LogSecurityEvent("PONG_ERROR", $"Error processing ping response: {ex.Message}", user);
        }
    }

    /// <summary>
    /// Send periodic ping requests to all connected clients
    /// </summary>
    public void SendPingToAllClients()
    {
        foreach (var connection in _masterServer.Connections)
        {
            if (connection.Status == NetConnectionStatus.Connected)
            {
                try
                {
                    var currentTime = GetCurrentTime();
                    var pingData = BitConverter.GetBytes(currentTime);
                    SendPacketToUser(connection, pingData, SfPacketType.Ping, NetDeliveryMethod.ReliableOrdered);
                }
                catch (Exception ex)
                {
                    LogSecurityEvent("PING_SEND_ERROR", $"Error sending ping: {ex.Message}", connection);
                }
            }
        }
    }

    /// <summary>
    /// Validates a projectile to prevent impossible trajectories or spam
    /// </summary>
    /// <param name="projectile">Projectile to validate</param>
    /// <param name="client">Client firing the projectile</param>
    /// <returns>True if projectile is valid</returns>
    private bool ValidateProjectile(ProjectilePackage projectile, ClientInfo client)
    {
        // Validate projectile position is reasonable relative to player position
        var playerPos = client.PositionInfo.Position;
        var projectilePos = projectile.ShootPosition;
        
        var distance = Math.Sqrt(
            Math.Pow(projectilePos.X - playerPos.Y, 2) + 
            Math.Pow(projectilePos.Y - playerPos.Z, 2));
            
        // Projectile should originate near the player (within reasonable range)
        const float maxProjectileOriginDistance = 20.0f;
        if (distance > maxProjectileOriginDistance)
        {
            LogSecurityEvent("PROJECTILE_TOO_FAR", 
                $"Projectile origin too far from player: {distance:F1} units from {client.Username}", null);
            return false;
        }
        
        // Validate projectile velocity is reasonable
        var velocity = projectile.ShootVector;
        var velocityMagnitude = Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
        
        const float maxProjectileVelocity = 200.0f;
        if (velocityMagnitude > maxProjectileVelocity)
        {
            LogSecurityEvent("PROJECTILE_TOO_FAST", 
                $"Projectile velocity too high: {velocityMagnitude:F1} from {client.Username}", null);
            return false;
        }
        
        // Check for NaN or infinite values
        if (float.IsNaN(projectilePos.X) || float.IsNaN(projectilePos.Y) ||
            float.IsNaN(velocity.X) || float.IsNaN(velocity.Y) ||
            float.IsInfinity(projectilePos.X) || float.IsInfinity(projectilePos.Y) ||
            float.IsInfinity(velocity.X) || float.IsInfinity(velocity.Y))
        {
            LogSecurityEvent("INVALID_PROJECTILE_VALUES", 
                $"NaN/Infinity projectile values from {client.Username}", null);
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Validates physics force values to prevent exploits
    /// </summary>
    /// <param name="forceData">Force data packet</param>
    /// <param name="client">Client applying the force</param>
    /// <returns>True if force values are valid</returns>
    private bool ValidatePhysicsForce(byte[] forceData, ClientInfo client)
    {
        if (forceData == null || forceData.Length < 8) // Need at least 8 bytes for force vector
        {
            return false;
        }

        try
        {
            // Read force values (assuming Vector2 force at start of packet)
            var forceX = BitConverter.ToSingle(forceData, 0);
            var forceY = BitConverter.ToSingle(forceData, 4);
            
            // Validate force magnitude
            var forceMagnitude = Math.Sqrt(forceX * forceX + forceY * forceY);
            const float maxForce = 1000f; // Reasonable maximum force
            
            if (forceMagnitude > maxForce)
            {
                LogSecurityEvent("EXCESSIVE_FORCE", 
                    $"Force magnitude {forceMagnitude:F1} exceeds limit from {client.Username}", null);
                return false;
            }
            
            // Check for NaN or infinite values
            if (float.IsNaN(forceX) || float.IsNaN(forceY) || 
                float.IsInfinity(forceX) || float.IsInfinity(forceY))
            {
                LogSecurityEvent("INVALID_FORCE_VALUES", 
                    $"NaN/Infinity force values from {client.Username}", null);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            LogSecurityEvent("FORCE_PARSE_ERROR", 
                $"Error parsing force data from {client.Username}: {ex.Message}", null);
            return false;
        }
    }

    /// <summary>
    /// Calculates server-side damage based on weapon type, distance, and game rules
    /// </summary>
    /// <param name="attacker">Attacking player</param>
    /// <param name="target">Target player</param>
    /// <param name="clientDamage">Damage amount sent by client (for reference/validation)</param>
    /// <returns>Server-calculated damage amount</returns>
    private float CalculateServerSideDamage(ClientInfo attacker, ClientInfo target, float clientDamage)
    {
        // Basic damage calculation based on weapon type
        var weaponType = attacker.WeaponInfo.WeaponType;
        float baseDamage = GetBaseDamageForWeapon(weaponType);
        
        // Calculate distance between players for damage falloff
        var distance = CalculateDistance(attacker.PositionInfo.Position, target.PositionInfo.Position);
        float distanceMultiplier = CalculateDistanceMultiplier(distance, weaponType);
        
        // Calculate final damage
        float calculatedDamage = baseDamage * distanceMultiplier;
        
        // Compare with client damage and apply anti-cheat logic
        float maxAllowedDeviation = baseDamage * 0.5f; // Allow 50% deviation for network lag/precision
        
        if (Math.Abs(calculatedDamage - clientDamage) > maxAllowedDeviation)
        {
            LogSecurityEvent("DAMAGE_MISMATCH", 
                $"Large damage deviation: server={calculatedDamage:F1}, client={clientDamage:F1} from {attacker.Username}", null);
        }
        
        // Use server calculation but cap it reasonably
        return Math.Min(calculatedDamage, 100f); // Max 100 damage per hit
    }
    
    /// <summary>
    /// Gets base damage for a weapon type
    /// </summary>
    private static float GetBaseDamageForWeapon(byte weaponType)
    {
        // Basic weapon damage values (adjust based on actual game balance)
        return weaponType switch
        {
            0 => 0f,    // No weapon/fists
            1 => 25f,   // Basic weapon
            2 => 35f,   // Medium weapon
            3 => 50f,   // Heavy weapon
            4 => 75f,   // Powerful weapon
            _ => 20f    // Default fallback
        };
    }
    
    /// <summary>
    /// Calculates distance between two positions
    /// </summary>
    private static float CalculateDistance(Vector3 pos1, Vector3 pos2)
    {
        var dy = pos1.Y - pos2.Y;
        var dz = pos1.Z - pos2.Z;
        return (float)Math.Sqrt(dy * dy + dz * dz);
    }
    
    /// <summary>
    /// Calculates damage multiplier based on distance and weapon type
    /// </summary>
    private static float CalculateDistanceMultiplier(float distance, byte weaponType)
    {
        // Different weapons have different effective ranges
        float maxEffectiveRange = weaponType switch
        {
            0 => 2f,    // Melee range
            1 => 10f,   // Short range
            2 => 15f,   // Medium range
            3 => 8f,    // Short range but high damage
            4 => 20f,   // Long range
            _ => 10f    // Default
        };
        
        // Linear falloff after effective range
        if (distance <= maxEffectiveRange)
        {
            return 1.0f;
        }
        else
        {
            // Damage falls off linearly to 25% at 2x effective range
            float falloffDistance = maxEffectiveRange * 2f;
            if (distance >= falloffDistance)
            {
                return 0.25f;
            }
            
            float falloffRatio = (distance - maxEffectiveRange) / maxEffectiveRange;
            return 1.0f - (falloffRatio * 0.75f);
        }
    }
    
    /// <summary>
    /// Creates a damage packet with server-calculated damage
    /// </summary>
    private byte[] CreateDamagePacket(int targetPlayerIndex, float damage)
    {
        var tempMsg = _masterServer.CreateMessage();
        tempMsg.Write(damage);
        // Add other damage-related data as needed by the game protocol
        return tempMsg.Data;
    }

    /// <summary>
    /// Validates player movement to prevent speed hacking and impossible movements
    /// </summary>
    /// <param name="previousPosition">Previous position</param>
    /// <param name="newPosition">New position</param>
    /// <param name="newRotation">New rotation</param>
    /// <param name="movementType">Movement type flags</param>
    /// <param name="client">Client info</param>
    /// <returns>True if movement is valid</returns>
    private bool ValidateMovement(Vector3 previousPosition, Vector3 newPosition, Vector2 newRotation, byte movementType, ClientInfo client)
    {
        // Allow initial position (first movement from spawn)
        if (previousPosition.Y == 0 && previousPosition.Z == 0)
        {
            return true;
        }

        // Calculate movement delta
        var deltaY = Math.Abs(newPosition.Y - previousPosition.Y);
        var deltaZ = Math.Abs(newPosition.Z - previousPosition.Z);
        var totalMovement = Math.Sqrt(deltaY * deltaY + deltaZ * deltaZ);

        // Maximum movement per update (assumes ~60 FPS, adjust as needed)
        // Stick Fight characters move relatively slowly, so this should be reasonable
        const float maxMovementPerUpdate = 5.0f; // Units per update
        
        if (totalMovement > maxMovementPerUpdate)
        {
            LogSecurityEvent("SPEED_HACK", $"Excessive movement: {totalMovement:F2} units from {client.Username}", null);
            return false;
        }

        // Basic bounds checking (adjust based on typical map sizes)
        const float maxCoordinate = 1000f;
        const float minCoordinate = -1000f;
        
        if (newPosition.Y < minCoordinate || newPosition.Y > maxCoordinate ||
            newPosition.Z < minCoordinate || newPosition.Z > maxCoordinate)
        {
            LogSecurityEvent("OUT_OF_BOUNDS", $"Position out of bounds: Y={newPosition.Y:F2}, Z={newPosition.Z:F2} from {client.Username}", null);
            return false;
        }

        // Validate rotation values (should be normalized)
        if (Math.Abs(newRotation.X) > 2.0f || Math.Abs(newRotation.Y) > 2.0f)
        {
            LogSecurityEvent("INVALID_ROTATION", $"Invalid rotation: X={newRotation.X:F2}, Y={newRotation.Y:F2} from {client.Username}", null);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Rebuilds a player update packet with validated data
    /// </summary>
    /// <param name="positionInfo">Validated position info</param>
    /// <param name="fightState">Fight state</param>
    /// <param name="weaponInfo">Weapon info</param>
    /// <returns>Byte array containing the rebuilt packet</returns>
    private byte[] RebuildPlayerUpdatePacket(PositionPackage positionInfo, byte fightState, WeaponPackage weaponInfo)
    {
        var tempMsg = _masterServer.CreateMessage();
        
        // Write position data
        tempMsg.Write((short)(positionInfo.Position.Y * 100f));
        tempMsg.Write((short)(positionInfo.Position.Z * 100f));
        tempMsg.Write((sbyte)(positionInfo.Rotation.X * 100f));
        tempMsg.Write((sbyte)(positionInfo.Rotation.Y * 100f));
        tempMsg.Write(positionInfo.YValue);
        tempMsg.Write(positionInfo.MovementType);
        
        // Write fight state
        tempMsg.Write(fightState);
        
        // Write projectiles
        var projectiles = weaponInfo.GetProjectilePackages();
        tempMsg.Write((ushort)projectiles.Length);
        
        foreach (var projectile in projectiles)
        {
            tempMsg.Write((short)projectile.ShootPosition.X);
            tempMsg.Write((short)projectile.ShootPosition.Y);
            tempMsg.Write((sbyte)projectile.ShootVector.X);
            tempMsg.Write((sbyte)projectile.ShootVector.Y);
            tempMsg.Write(projectile.SyncIndex);
        }
        
        // Write weapon type
        tempMsg.Write(weaponInfo.WeaponType);
        
        return tempMsg.Data;
    }

    /// <summary>
    /// Security: Log security events
    /// </summary>
    /// <param name="eventType">Type of security event</param>
    /// <param name="details">Event details</param>
    /// <param name="connection">Associated connection if any</param>
    private static void LogSecurityEvent(string eventType, string details, NetConnection connection = null)
    {
        var logMessage = $"[SECURITY] {eventType}: {details}";
        if (connection != null)
        {
            logMessage += $" from {connection.RemoteEndPoint}";
        }
        Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {logMessage}");
    }

    /// <summary>
    /// Handles when a client has joined the server.
    /// </summary>
    /// <param name="user">The client connection</param>
    /// <param name="msg">The join message</param>
    public void OnClientJoined(NetConnection user, NetIncomingMessage msg)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        if (client != null && Config.EnableLogging)
        {
            Log($"Client joined: {client.Username} ({client.SteamID})");
        }
    }

    /// <summary>
    /// Handles when a client has been accepted by the server.
    /// </summary>
    /// <param name="user">The client connection</param>
    /// <param name="msg">The acceptance message</param>
    public void OnClientAcceptedByServer(NetConnection user, NetIncomingMessage msg)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        if (client != null && Config.EnableLogging)
        {
            Log($"Client accepted by server: {client.Username} ({client.SteamID})");
        }
    }

    /// <summary>
    /// Handles when a player has spawned in the game.
    /// </summary>
    /// <param name="user">The client connection</param>
    /// <param name="msg">The spawn message</param>
    public void OnPlayerSpawned(NetConnection user, NetIncomingMessage msg)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        if (client != null)
        {
            client.Revive(); // Mark player as alive
            if (Config.EnableLogging)
            {
                Log($"Player spawned: {client.Username} at position data");
            }
        }
    }

    /// <summary>
    /// Handles when a client indicates they are ready to start the game.
    /// </summary>
    /// <param name="user">The client connection</param>
    /// <param name="msg">The ready up message</param>
    public void OnClientReadyUp(NetConnection user, NetIncomingMessage msg)
    {
        var client = _clientMgr.GetClient(user.RemoteEndPoint.Address);
        if (client != null && Config.EnableLogging)
        {
            Log($"Client ready up: {client.Username} ({client.SteamID})");
        }
        
        // Check if all clients are ready and potentially start the game
        var readyClients = _clientMgr.AllClients.Count(c => c != null);
        if (Config.EnableLogging)
        {
            Log($"Ready clients: {readyClients}/{Config.MaxPlayers}");
        }
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
            // Close logging resources
            if (_config.EnableLogging)
            {
                LogToFile($"=== SF-Server session ended at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===");
            }
            
            _logWriter?.Dispose();
            _logFileStream?.Dispose();
            
            _httpClient?.Dispose();
            _masterServer?.Shutdown("Server shutting down");
        }
    }
}
