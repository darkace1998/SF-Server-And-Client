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

            // For data messages, check the channel to determine if it's player data
            var channel = msg.SequenceChannel;
            Debug.Log($"Data message received with channel: {channel}, type: {msg.MessageType}");

            if (channel is > -2 and < 2 or > 9)//  Don't want NetworkPlayer updates going through the normal p2p handler
            {
                __result = msg;
                return false;
            }

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

    private static void SetRunningOnSockets(bool isOnSockets)
        => AccessTools.Property(typeof(MatchmakingHandler), nameof(MatchmakingHandler.RunningOnSockets))
            .SetValue(null, // obj instance is null because property is static
                isOnSockets,
                BindingFlags.Default,
                null,
                null,
                null!);
}
