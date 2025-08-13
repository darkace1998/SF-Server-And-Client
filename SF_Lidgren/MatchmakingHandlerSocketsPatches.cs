using System;
using System.Reflection;
using HarmonyLib;
using Lidgren.Network;
using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public static class MatchmakingHandlerSocketsPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var readMessageMethod = AccessTools.Method(typeof(MatchMakingHandlerSockets), "ReadMessage");
        var readMessageMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerSocketsPatches)
            .GetMethod(nameof(ReadMessageMethodPrefix)));

        var joinServerMethod = AccessTools.Method(typeof(MatchMakingHandlerSockets), nameof(MatchMakingHandlerSockets.JoinServer));
        var joinServerMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerSocketsPatches)
            .GetMethod(nameof(JoinServerMethodPrefix)));

        var joinServerAtMethod = AccessTools.Method(typeof(MatchMakingHandlerSockets), nameof(MatchMakingHandlerSockets.JoinServerAt));
        var joinServerAtMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerSocketsPatches)
            .GetMethod(nameof(JoinServerMethodAtPrefix)));

        harmonyInstance.Patch(readMessageMethod, prefix: readMessageMethodPrefix);
        harmonyInstance.Patch(joinServerMethod, prefix: joinServerMethodPrefix);
        harmonyInstance.Patch(joinServerAtMethod, prefix: joinServerAtMethodPrefix);
    }

    public static bool ReadMessageMethodPrefix(ref bool ___m_Active, ref NetClient ___m_Client, ref NetIncomingMessage __result)
    {
        NetIncomingMessage msg;
        __result = null;

        if (!___m_Active) return false;
        
        try
        {
            if ((msg = ___m_Client.ReadMessage()) == null) return false;

            // Handle system messages (connection status, approval, etc.) by passing them to original handler
            if (msg.MessageType == NetIncomingMessageType.StatusChanged ||
                msg.MessageType == NetIncomingMessageType.ConnectionApproval ||
                msg.MessageType == NetIncomingMessageType.DiscoveryResponse ||
                msg.MessageType == NetIncomingMessageType.Error ||
                msg.MessageType == NetIncomingMessageType.VerboseDebugMessage ||
                msg.MessageType == NetIncomingMessageType.DebugMessage ||
                msg.MessageType == NetIncomingMessageType.WarningMessage ||
                msg.MessageType == NetIncomingMessageType.ErrorMessage)
            {
                Debug.Log($"System message received: {msg.MessageType}");
                __result = msg;
                return false; // Let the original method process system messages
            }

            // For data messages, check if this is a server-sent multiplayer packet that needs special handling
            var channel = msg.SequenceChannel;
            Debug.Log($"Data message received with channel: {channel}, type: {msg.MessageType}");

            // Read the packet type to determine how to handle it
            var originalPosition = msg.Position;
            var timeSent = msg.ReadUInt32();
            var serverPacketType = msg.ReadByte();
            
            // Handle important server packets that affect multiplayer state
            if (ShouldHandleServerPacket(serverPacketType))
            {
                Debug.Log($"Processing server packet type: {serverPacketType}");
                ProcessServerPacket(serverPacketType, msg, timeSent);
                ___m_Client.Recycle(msg);
                NetworkUtils.IncrementPacketsReceived();
                return false;
            }
            
            // Reset message position since we peeked at the data
            msg.Position = originalPosition;

            // Check if this is a player-specific channel for NetworkPlayer updates/events
            if (channel is > 1 and <= 9) // Player channels are 2-9 (2,3 for player 0; 4,5 for player 1; etc.)
            {
                Debug.Log("Packet is meant for NetworkPlayer!");
                var isUpdateChannel = channel % 2 == 0; // Whether channel is update or event channel
                int senderPlayerID;

                if (isUpdateChannel)
                {
                    senderPlayerID = (channel - 2) / 2;
                    // Add bounds checking to prevent array access exception
                    if (senderPlayerID >= 0 && senderPlayerID < NetworkUtils.PlayerUpdatePackets.Length)
                    {
                        NetworkUtils.PlayerUpdatePackets[senderPlayerID] = msg;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid senderPlayerID {senderPlayerID} for update channel {channel}");
                    }
                    return false;
                }

                Console.WriteLine($"Adding msg with channel {channel} to event packets array!");
                senderPlayerID = (channel - 3) / 2;
                // Add bounds checking to prevent array access exception
                if (senderPlayerID >= 0 && senderPlayerID < NetworkUtils.PlayerEventPackets.Length)
                {
                    NetworkUtils.PlayerEventPackets[senderPlayerID] = msg;
                }
                else
                {
                    Debug.LogWarning($"Invalid senderPlayerID {senderPlayerID} for event channel {channel}");
                }
                return false;
            }

            // For other messages, pass to original handler (like ClientInit, etc.)
            __result = msg;
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in ReadMessageMethodPrefix: {ex.Message}");
            NetworkUtils.IncrementPacketsReceived(); // Still count it to maintain statistics
            return false; // Don't let the original method run if we had an error
        }
    }

    public static bool JoinServerMethodPrefix(ref bool ___m_Active, ref bool ___m_IsServer, ref NetClient ___m_Client,
        ref NetConnection ___m_NetConnection)
    {
        try
        {
            ___m_Active = true;
            ___m_IsServer = false;
            SetRunningOnSockets(true);
            Console.WriteLine("Matchmaking running on sockets?: " + MatchmakingHandler.RunningOnSockets);

            var netPeerConfiguration = new NetPeerConfiguration(Plugin.AppIdentifier);

            netPeerConfiguration.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            netPeerConfiguration.EnableMessageType(NetIncomingMessageType.StatusChanged);
            netPeerConfiguration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            var netClient = new NetClient(netPeerConfiguration);
            netClient.Start();
            ___m_Client = netClient;
            
            // Validate connection parameters
            if (string.IsNullOrEmpty(TempGUI.Address) || TempGUI.Address.Trim().Length == 0)
            {
                Debug.LogError("Cannot connect: Address is empty");
                return false;
            }
            
            if (TempGUI.Port <= 0 || TempGUI.Port > 65535)
            {
                Debug.LogError($"Cannot connect: Invalid port {TempGUI.Port}");
                return false;
            }
            
            var discoveredPeer = ___m_Client.DiscoverKnownPeer(TempGUI.Address, TempGUI.Port);
            Debug.Log("Did discover server at address: " + discoveredPeer);

            // Client-side auth work: get and send ticket to server for verification
            var ticketByteArray = new byte[1024];
            
            // Check if Steam API is available before using it
            if (!SteamAPI.IsSteamRunning())
            {
                Debug.LogError("Steam API is not running, cannot get auth ticket");
                return false;
            }
            
            var ticketHandler = SteamUser.GetAuthSessionTicket(ticketByteArray, ticketByteArray.Length, out var ticketSize);
            
            if (ticketHandler == HAuthTicket.Invalid)
            {
                Debug.LogError("Failed to get valid Steam auth ticket");
                return false;
            }
            
            Array.Resize(ref ticketByteArray, (int)ticketSize);

            var onConnectMsg = ___m_Client.CreateMessage();
            onConnectMsg.Write(ticketByteArray);

            // Attempt to connect to server
            ___m_NetConnection = ___m_Client.Connect(TempGUI.Address, TempGUI.Port, onConnectMsg);
            
            if (___m_NetConnection == null)
            {
                Debug.LogError("Failed to create network connection");
                return false;
            }
            
            NetworkUtils.LidgrenData = new LidgrenData(ticketHandler, ___m_Client, ___m_NetConnection);
            NetworkUtils.SetConnecting(true);

            Debug.Log($"Connection attempt initiated to {TempGUI.Address}:{TempGUI.Port}");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during JoinServer: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            
            // Clean up on error
            try
            {
                ___m_Client?.Shutdown("Connection failed");
            }
            catch (System.Exception cleanupEx)
            {
                Debug.LogError($"Error during cleanup: {cleanupEx.Message}");
            }
            
            NetworkUtils.SetError($"Connection failed: {ex.Message}");
            return false;
        }
    }

    public static bool JoinServerMethodAtPrefix() => false;

    /// <summary>
    /// Determines if a server packet type should be handled specially in socket mode
    /// </summary>
    /// <param name="packetType">The server packet type byte</param>
    /// <returns>True if this packet needs special handling</returns>
    private static bool ShouldHandleServerPacket(byte packetType)
    {
        // These correspond to SfPacketType enum values from the server
        // We need to handle packets that affect multiplayer state
        return packetType switch
        {
            2 => true,  // ClientJoined
            8 => true,  // ClientSpawned  
            9 => true,  // ClientReadyUp
            18 => true, // MapChange
            _ => false
        };
    }

    /// <summary>
    /// Processes important server packets that affect multiplayer state
    /// </summary>
    /// <param name="packetType">The server packet type</param>
    /// <param name="msg">The message containing packet data</param>
    /// <param name="timeSent">The timestamp when packet was sent</param>
    private static void ProcessServerPacket(byte packetType, NetIncomingMessage msg, uint timeSent)
    {
        try
        {
            var multiplayerManager = GameManager.Instance?.mMultiplayerManager;
            if (multiplayerManager == null)
            {
                Debug.LogWarning("MultiplayerManager not available for server packet processing");
                return;
            }

            var packetData = msg.ReadBytes(msg.LengthBytes - 5); // Read remaining data after timestamp and type

            switch (packetType)
            {
                case 2: // ClientJoined
                    Debug.Log("Processing ClientJoined packet from server");
                    ProcessClientJoinedPacket(multiplayerManager, packetData);
                    break;
                    
                case 8: // ClientSpawned
                    Debug.Log("Processing ClientSpawned packet from server");
                    ProcessClientSpawnedPacket(multiplayerManager, packetData);
                    break;
                    
                case 9: // ClientReadyUp
                    Debug.Log("Processing ClientReadyUp packet from server");
                    // Could implement ready up UI updates here
                    break;
                    
                case 18: // MapChange
                    Debug.Log("Processing MapChange packet from server");
                    ProcessMapChangePacket(multiplayerManager, packetData);
                    break;
                    
                default:
                    Debug.LogWarning($"Unhandled server packet type: {packetType}");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing server packet type {packetType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles ClientJoined packets to ensure other players are visible
    /// </summary>
    private static void ProcessClientJoinedPacket(MultiplayerManager multiplayerManager, byte[] packetData)
    {
        try
        {
            if (packetData.Length < 9) // playerIndex (1) + steamId (8)
            {
                Debug.LogWarning("ClientJoined packet too short");
                return;
            }

            var playerIndex = packetData[0];
            var steamId = BitConverter.ToUInt64(packetData, 1);
            
            Debug.Log($"Player {playerIndex} joined with Steam ID: {steamId}");
            
            // Try to trigger the multiplayer manager to handle the new player
            // Use reflection to call the appropriate method since it might be private
            var onClientJoinedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnClientJoined");
            if (onClientJoinedMethod != null)
            {
                Debug.Log($"Calling OnClientJoined for player {playerIndex}");
                onClientJoinedMethod.Invoke(multiplayerManager, new object[] { packetData });
            }
            else
            {
                Debug.LogWarning("OnClientJoined method not found, trying alternative approach");
                
                // Alternative: Manually update the connected clients array
                var connectedClientsField = AccessTools.Field(typeof(MultiplayerManager), "mConnectedClients");
                if (connectedClientsField != null)
                {
                    var connectedClients = (ConnectedClientData[])connectedClientsField.GetValue(multiplayerManager);
                    if (connectedClients != null && playerIndex < connectedClients.Length)
                    {
                        // Create a new connected client entry if it doesn't exist
                        if (connectedClients[playerIndex] == null)
                        {
                            Debug.Log($"Creating new ConnectedClientData for player {playerIndex}");
                            // Note: We'd need to create a proper ConnectedClientData instance here
                            // but without knowing the exact structure, we'll log this for now
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing ClientJoined packet: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles ClientSpawned packets to ensure other players spawn visually
    /// </summary>
    private static void ProcessClientSpawnedPacket(MultiplayerManager multiplayerManager, byte[] packetData)
    {
        try
        {
            if (packetData.Length < 26) // Expected spawn data length
            {
                Debug.LogWarning("ClientSpawned packet too short");
                return;
            }

            Debug.Log("Processing spawn data for player");
            
            // Try to call the multiplayer manager's spawn handling method
            var onPlayerSpawnedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnPlayerSpawned");
            if (onPlayerSpawnedMethod != null)
            {
                Debug.Log("Calling OnPlayerSpawned through MultiplayerManager");
                onPlayerSpawnedMethod.Invoke(multiplayerManager, new object[] { packetData });
            }
            else
            {
                Debug.LogWarning("OnPlayerSpawned method not found on MultiplayerManager");
                
                // Alternative: Trigger spawn through NetworkPlayer system
                var playerIndex = packetData[0]; // First byte should be player index
                Debug.Log($"Triggering spawn for player index: {playerIndex}");
                
                // Find the appropriate NetworkPlayer and trigger spawn
                var networkPlayers = UnityEngine.Object.FindObjectsOfType<NetworkPlayer>();
                foreach (var networkPlayer in networkPlayers)
                {
                    var spawnIdField = AccessTools.Field(typeof(NetworkPlayer), "mNetworkSpawnID");
                    if (spawnIdField != null)
                    {
                        var spawnId = (ushort)spawnIdField.GetValue(networkPlayer);
                        if (spawnId == playerIndex)
                        {
                            Debug.Log($"Found NetworkPlayer for spawn ID {playerIndex}, triggering spawn");
                            
                            // Try to make the player active/visible
                            var setActiveMethod = AccessTools.Method(typeof(NetworkPlayer), "SetActive");
                            if (setActiveMethod != null)
                            {
                                setActiveMethod.Invoke(networkPlayer, new object[] { true });
                            }
                            
                            break;
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing ClientSpawned packet: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles MapChange packets from the server
    /// </summary>
    private static void ProcessMapChangePacket(MultiplayerManager multiplayerManager, byte[] packetData)
    {
        try
        {
            if (packetData.Length < 3) // winnerIndex + mapType + mapData
            {
                Debug.LogWarning("MapChange packet too short");
                return;
            }

            var winnerIndex = packetData[0];
            var mapType = packetData[1];
            
            Debug.Log($"Map change: Winner={winnerIndex}, MapType={mapType}");
            
            // Try to call the map change method
            var onMapChangedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnMapChanged");
            if (onMapChangedMethod != null)
            {
                onMapChangedMethod.Invoke(multiplayerManager, new object[] { packetData });
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing MapChange packet: {ex.Message}");
        }
    }

    private static void SetRunningOnSockets(bool isOnSockets)
        => AccessTools.Property(typeof(MatchmakingHandler), nameof(MatchmakingHandler.RunningOnSockets))
            .SetValue(null, // obj instance is null because property is static
                isOnSockets,
                BindingFlags.Default,
                null,
                null,
                null!);
}
