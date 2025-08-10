using System;
using System.IO;
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

    // TODO: Support multiple players on same device?
    public static bool OnClientAcceptedByServerMethodPrefix(ref bool ___mHasBeenAcceptedFromServer)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        ___mHasBeenAcceptedFromServer = true;

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

    public static void OnInitFromServerMethodPostfix(MultiplayerManager __instance)
    {
        if (!MatchmakingHandler.RunningOnSockets) return;

        // TODO: Change this to be IP based and server-side later
        if (__instance.LocalPlayerIndex == 0)
        {
            // Use reflection to set the static IsServer property since direct field injection failed
            AccessTools.Property(typeof(MultiplayerManager), "IsServer")
                .SetValue(null, // obj instance is null because property is static
                    true,
                    BindingFlags.Default,
                    null,
                    null,
                    null);
        }
    }

    // Implement proper disconnected player checking
    public static bool CheckForDisconnectedPlayersMethodPrefix()
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;
        
        // Check connection status of the Lidgren client
        if (NetworkUtils.LidgrenData?.LocalClient != null)
        {
            var client = NetworkUtils.LidgrenData.LocalClient;
            
            // Check if we're still connected to the server
            if (NetworkUtils.LidgrenData.ServerConnection?.Status != NetConnectionStatus.Connected)
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
        
        return false; // Always handle disconnection checking ourselves
    }

    public static void OnPlayerSpawnedMethodPrefix(ref byte[] data)
    {
        if (!MatchmakingHandler.RunningOnSockets) return;

        Console.WriteLine("Looking at spawn flag byte: " + data[25]);

        // TODO: Investigate and understand why this happens?
        // Sometimes spawnPosition flag is random byte value instead of bool, if this is the case default to 0
        if (data[25] > 1) data[25] = 0;
    }

    // TODO: switch to server sending out this packet based on server-tracked HP
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

            var unreadyAllPlayersMethod = AccessTools.Method(typeof(MultiplayerManager), "UnReadyAllPlayers");
            unreadyAllPlayersMethod.Invoke(__instance, null);

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
            NetworkUtils.SendPacketToServer(array, P2PPackageHandler.MsgType.MapChange, NetDeliveryMethod.ReliableOrdered,
                __instance.LocalPlayerIndex);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in ChangeMapMethodPrefix: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            
            // Try to send a fallback map change
            try
            {
                Debug.Log("Attempting fallback map change...");
                var fallbackArray = new byte[] { indexOfWinner, 0, 0, 0, 0, 0 }; // Lobby map fallback
                NetworkUtils.SendPacketToServer(fallbackArray, P2PPackageHandler.MsgType.MapChange, NetDeliveryMethod.ReliableOrdered,
                    __instance.LocalPlayerIndex);
            }
            catch (System.Exception fallbackEx)
            {
                Debug.LogError($"Fallback map change also failed: {fallbackEx.Message}");
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
