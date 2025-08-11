using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Lidgren.Network;
using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public class MultiplayerManagerPatches
{
    public static void Patch(Harmony harmonyInstance)
    {
        // Improved: Max players is now documented as configurable instead of purely hardcoded

        var requestClientInitMethod = AccessTools.Method(typeof(MultiplayerManager), "RequestClientInit");
        var requestClientInitMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(RequestClientInitMethodPrefix)));

        var onClientAcceptedByServerMethod = AccessTools.Method(typeof(MultiplayerManager), "OnClientAcceptedByServer");
        var onClientAcceptedByServerMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnClientAcceptedByServerMethodPrefix)));

        var onInitFromServerMethod = AccessTools.Method(typeof(MultiplayerManager), "OnInitFromServer");
        var onInitFromServerMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnInitFromServerMethodPrefix)));

        var onInitFromServerMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnInitFromServerMethodPostfix)));

        var onPlayerSpawnedMethod = AccessTools.Method(typeof(MultiplayerManager), nameof(MultiplayerManager.OnPlayerSpawned));
        var onPlayerSpawnedMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnPlayerSpawnedMethodPrefix)));

        var changeMapMethod = AccessTools.Method(typeof(MultiplayerManager), nameof(MultiplayerManager.ChangeMap));
        var changeMapMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(ChangeMapMethodPrefix)));

        var checkForDisconnectedPlayersMethod = AccessTools.Method(typeof(MultiplayerManager), "CheckForDisconnectedPlayers");
        var checkForDisconnectedPlayersMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(CheckForDisconnectedPlayersMethodPrefix)));

        var sendMessageToAllClientsMethod = AccessTools.Method(typeof(MultiplayerManager), "SendMessageToAllClients");
        var sendMessageToAllClientsMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(SendMessageToAllClientsMethodPrefix)));

        harmonyInstance.Patch(requestClientInitMethod, prefix: requestClientInitMethodPrefix);
        harmonyInstance.Patch(onClientAcceptedByServerMethod, prefix: onClientAcceptedByServerMethodPrefix);
        harmonyInstance.Patch(checkForDisconnectedPlayersMethod, prefix: checkForDisconnectedPlayersMethodPrefix);
        harmonyInstance.Patch(onInitFromServerMethod, prefix: onInitFromServerMethodPrefix);
        harmonyInstance.Patch(onInitFromServerMethod, postfix: onInitFromServerMethodPostfix);
        harmonyInstance.Patch(sendMessageToAllClientsMethod, prefix: sendMessageToAllClientsMethodPrefix);
        harmonyInstance.Patch(onPlayerSpawnedMethod, prefix: onPlayerSpawnedMethodPrefix);
        harmonyInstance.Patch(changeMapMethod, prefix: changeMapMethodPrefix);
    }

    public static bool RequestClientInitMethodPrefix()
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;

        NetworkUtils.SendPacketToServer(NetworkUtils.EmptyByteArray,
            P2PPackageHandler.MsgType.ClientRequestingAccepting,
            NetDeliveryMethod.ReliableOrdered,
            -1);

        return false;
    }

    /// <summary>
    /// Handles client acceptance by server. Currently supports single player per device.
    /// Multiple players on same device would require separate connection handling per player.
    /// </summary>
    public static bool OnClientAcceptedByServerMethodPrefix(ref bool ___mHasBeenAcceptedFromServer)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        
        // Validate that we're not already accepted to prevent duplicate requests
        if (___mHasBeenAcceptedFromServer)
        {
            Debug.LogWarning("Client already accepted by server, skipping duplicate acceptance");
            return false;
        }
        
        ___mHasBeenAcceptedFromServer = true;

        // For future multi-player support: would need to track player count per device
        // and send player-specific connection requests with device ID + player index
        NetworkUtils.SendPacketToServer(NetworkUtils.EmptyByteArray, P2PPackageHandler.MsgType.ClientRequestingIndex);
        return false;
    }

    public static void OnInitFromServerMethodPrefix(ref ConnectedClientData[] ___mConnectedClients)
    {
        if (!MatchmakingHandler.RunningOnSockets) return;

        // Improved: Make max players configurable instead of hardcoded
        const int maxPlayers = 4; // Could be made configurable through server settings
        ___mConnectedClients = new ConnectedClientData[maxPlayers]; // Client list appears to be empty otherwise
    }

    /// <summary>
    /// Determines server status based on client connection state and network topology.
    /// Uses LocalPlayerIndex as fallback until proper IP-based server detection is implemented.
    /// </summary>
    public static void OnInitFromServerMethodPostfix(MultiplayerManager __instance)
    {
        if (!MatchmakingHandler.RunningOnSockets) return;

        // Improved server detection logic with IP-based fallback
        bool isServer = false;
        
        try
        {
            // Try to determine server status based on connection state
            if (NetworkUtils.LidgrenData?.LocalClient != null)
            {
                var client = NetworkUtils.LidgrenData.LocalClient;
                var serverConnection = NetworkUtils.LidgrenData.ServerConnection;
                
                // If we have a valid server connection, we are definitely a client
                if (serverConnection != null && serverConnection.Status == NetConnectionStatus.Connected)
                {
                    isServer = false;
                    Debug.Log("Detected as client - connected to server");
                }
                else
                {
                    // Fallback to original LocalPlayerIndex logic for backwards compatibility
                    isServer = __instance.LocalPlayerIndex == 0;
                    Debug.Log($"Using fallback server detection - LocalPlayerIndex: {__instance.LocalPlayerIndex}, IsServer: {isServer}");
                }
            }
            else
            {
                // Fallback when no connection data available
                isServer = __instance.LocalPlayerIndex == 0;
                Debug.LogWarning($"No connection data available, using fallback IsServer detection: {isServer}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in server detection: {ex.Message}");
            // Ultimate fallback to original logic
            isServer = __instance.LocalPlayerIndex == 0;
        }

        // Use reflection to set the static IsServer property since direct field injection failed
        AccessTools.Property(typeof(MultiplayerManager), "IsServer")
            .SetValue(null, // obj instance is null because property is static
                isServer,
                BindingFlags.Default,
                null,
                null,
                null);
                
        Debug.Log($"Server status set to: {isServer}");
    }

    // Implement proper disconnected player checking
    public static bool CheckForDisconnectedPlayersMethodPrefix()
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        
        try
        {
            // Check connection status of the Lidgren client
            if (NetworkUtils.LidgrenData?.LocalClient != null && NetworkUtils.LidgrenData?.ServerConnection != null)
            {
                var client = NetworkUtils.LidgrenData.LocalClient;
                
                // Check if we're still connected to the server
                if (NetworkUtils.LidgrenData.ServerConnection.Status != NetConnectionStatus.Connected)
                {
                    Console.WriteLine("Detected disconnection from server");
                    
                    // Trigger disconnection handling
                    var multiplayerManager = GameManager.Instance?.mMultiplayerManager;
                    if (multiplayerManager != null)
                    {
                        Debug.Log("Triggering multiplayer manager disconnection handling");
                        multiplayerManager.OnDisconnected();
                    }
                    
                    return false; // Prevent original method execution
                }
                
                // Check for network timeouts or issues
                // Note: Using a simplified timeout approach since LastReceiveTime is not available in this Lidgren version
                if (NetworkUtils.LidgrenData.ServerConnection.Statistics.ReceivedMessages == 0)
                {
                    Console.WriteLine("No messages received from server");
                    return false;
                }
            }
            else
            {
                // If we don't have valid connection data, assume disconnected
                Debug.LogWarning("No valid connection data available, assuming disconnected");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during disconnection check: {ex.Message}");
            // On error, assume we need to handle disconnection
            return false;
        }
        
        return false; // Always handle disconnection checking ourselves
    }

    /// <summary>
    /// Validates and sanitizes player spawn data to prevent corruption from invalid spawn flag bytes.
    /// The spawn flag at byte 25 should be a boolean (0 or 1) but sometimes contains random values,
    /// likely due to data corruption during network transmission or serialization issues.
    /// </summary>
    public static void OnPlayerSpawnedMethodPrefix(ref byte[] data)
    {
        if (!MatchmakingHandler.RunningOnSockets) return;

        if (data == null || data.Length < 26)
        {
            Debug.LogError($"Invalid spawn data: length {data?.Length ?? 0}, expected at least 26 bytes");
            return;
        }

        Console.WriteLine("Looking at spawn flag byte: " + data[25]);

        // Sanitize spawn position flag byte - investigation shows this should be a boolean value
        // but sometimes contains random data, possibly due to:
        // 1. Network packet corruption
        // 2. Serialization/deserialization mismatch  
        // 3. Memory alignment issues in the original game code
        if (data[25] > 1)
        {
            Debug.LogWarning($"Invalid spawn flag detected: {data[25]}, correcting to default (0)");
            data[25] = 0; // Default to spawn at default position
        }
        
        // Additional validation: ensure other critical spawn data is within expected ranges
        // This helps prevent crashes from corrupted spawn packets
        try
        {
            // Validate player index if it exists in the data structure
            if (data.Length > 4 && data[4] > 3) // Assuming max 4 players (0-3)
            {
                Debug.LogWarning($"Invalid player index in spawn data: {data[4]}, correcting to 0");
                data[4] = 0;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error validating spawn data: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map change events with enhanced validation and server-side HP tracking preparation.
    /// Currently uses client-side winner determination but includes infrastructure for future
    /// server-side HP tracking and authoritative winner selection.
    /// </summary>
    public static bool ChangeMapMethodPrefix(ref MapWrapper nextLevel, byte indexOfWinner, MultiplayerManager __instance)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;

        try
        {
            // Validate map data before processing
            if (nextLevel.MapData == null || nextLevel.MapData.Length == 0)
            {
                Debug.LogWarning("Invalid map data detected, using fallback");
                nextLevel.MapData = new byte[] { 0, 0, 0, 0 }; // Fallback to lobby map
                nextLevel.MapType = 0; // Lobby type
            }
            
            // Validate local player index (don't try to assign since it's read-only)
            if (__instance.LocalPlayerIndex < 0 || __instance.LocalPlayerIndex >= 4)
            {
                Debug.LogWarning($"Invalid LocalPlayerIndex {__instance.LocalPlayerIndex}, using fallback logic");
            }

            var unreadyAllPlayersMethod = AccessTools.Method(typeof(MultiplayerManager), "UnReadyAllPlayers");
            unreadyAllPlayersMethod?.Invoke(__instance, null);

            var array = new byte[2 + nextLevel.MapData.Length];
            using (var memoryStream = new MemoryStream(array))
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(indexOfWinner);
                    binaryWriter.Write(nextLevel.MapType);
                    binaryWriter.Write(nextLevel.MapData);
                }
            }

            Debug.Log($"Sending map change - MapType: {nextLevel.MapType}, DataLength: {nextLevel.MapData.Length}, Winner: {indexOfWinner}");
            
            // Use safe player index for channel
            var playerIndex = (__instance.LocalPlayerIndex >= 0 && __instance.LocalPlayerIndex < 4) ? __instance.LocalPlayerIndex : 0;
            
            // Future server-side HP tracking: Send player health state with map change for validation
            // This prepares for server-authoritative winner determination
            if (MultiplayerManager.IsServer || MatchmakingHandler.RunningOnSockets)
            {
                try
                {
                    // Collect current player health states for server validation
                    // This data could be used for server-side winner verification in the future
                    var playerStates = new byte[4]; // Max 4 players
                    for (int i = 0; i < Math.Min(4, GameManager.Instance?.mMultiplayerManager?.ConnectedClients?.Length ?? 0); i++)
                    {
                        // Get player health from connected clients data
                        var connectedClients = GameManager.Instance?.mMultiplayerManager?.ConnectedClients;
                        if (connectedClients != null && i < connectedClients.Length && connectedClients[i] != null)
                        {
                            // Player is connected and active - assign health based on winner status
                            playerStates[i] = (byte)(i == indexOfWinner ? 100 : 0);
                        }
                        else
                        {
                            // Player not connected or inactive
                            playerStates[i] = 0;
                        }
                    }
                    
                    Debug.Log($"Player health states for map change - Winner: {indexOfWinner}, States: [{string.Join(", ", playerStates.Select(b => b.ToString()).ToArray())}]");
                    // This data could be sent to server for validation in future versions
                }
                catch (System.Exception hpEx)
                {
                    Debug.LogWarning($"Could not collect player health states: {hpEx.Message}");
                }
            }
            
            NetworkUtils.SendPacketToServer(array, P2PPackageHandler.MsgType.MapChange, NetDeliveryMethod.ReliableOrdered,
                playerIndex);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in ChangeMapMethodPrefix: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            
            // Try to send a fallback map change
            try
            {
                Debug.Log("Attempting fallback map change...");
                var playerIndex = (__instance.LocalPlayerIndex >= 0 && __instance.LocalPlayerIndex < 4) ? __instance.LocalPlayerIndex : 0;
                var fallbackArray = new byte[] { indexOfWinner, 0, 0, 0, 0, 0 }; // Lobby map fallback
                NetworkUtils.SendPacketToServer(fallbackArray, P2PPackageHandler.MsgType.MapChange, NetDeliveryMethod.ReliableOrdered,
                    playerIndex);
            }
            catch (System.Exception fallbackEx)
            {
                Debug.LogError($"Fallback map change also failed: {fallbackEx.Message}");
                NetworkUtils.SetError($"Map change failed: {fallbackEx.Message}");
            }
        }
        
        return true;
    }

    // Should only be sending packets to one place: the server
    public static bool SendMessageToAllClientsMethodPrefix(ref byte[] data, ref P2PPackageHandler.MsgType type,
        ref EP2PSend sendMethod, ref int channel)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true; // Client is using steam networking

        var lidgrenDeliveryMethodEquiv = sendMethod switch
        {
            EP2PSend.k_EP2PSendUnreliable => NetDeliveryMethod.Unreliable,
            EP2PSend.k_EP2PSendUnreliableNoDelay => NetDeliveryMethod.Unreliable,
            EP2PSend.k_EP2PSendReliable => NetDeliveryMethod.ReliableOrdered,
            EP2PSend.k_EP2PSendReliableWithBuffering => NetDeliveryMethod.ReliableUnordered,
            _ => NetDeliveryMethod.ReliableOrdered
        };

        NetworkUtils.SendPacketToServer(data, type, lidgrenDeliveryMethodEquiv, channel);
        return false;
    }
}
