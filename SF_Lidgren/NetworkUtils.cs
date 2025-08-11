using System;
using Lidgren.Network;
using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public static class NetworkUtils
{
    public static LidgrenData LidgrenData;

    // Improved packet storage arrays with better management (keeping arrays for .NET 3.5 compatibility)
    public static NetIncomingMessage[] PlayerUpdatePackets = new NetIncomingMessage[4]; // For holding packets meant for update channel
    public static NetIncomingMessage[] PlayerEventPackets = new NetIncomingMessage[4]; // For holding packets meant for event channel
    public static readonly byte[] EmptyByteArray = new byte[0];

    // Connection state tracking
    public static bool IsConnecting { get; private set; }
    public static bool IsConnected => LidgrenData?.ServerConnection?.Status == NetConnectionStatus.Connected;
    public static string LastError { get; private set; }

    // Statistics
    public static float ConnectionTime { get; private set; }
    public static int PacketsSent { get; private set; }
    public static int PacketsReceived { get; private set; }

    /// <summary>
    /// Validates and sanitizes string input to prevent enum parsing issues
    /// </summary>
    /// <param name="input">Input string to validate</param>
    /// <param name="fallback">Fallback value if input is invalid</param>
    /// <returns>Sanitized string</returns>
    public static string SanitizeStringInput(string input, string fallback = "")
    {
        if (string.IsNullOrEmpty(input))
            return fallback;
            
        // Remove potentially problematic characters
        var sanitized = input.Trim();
        
        // Check for known problematic values
        if (sanitized.Equals("blubb", System.StringComparison.OrdinalIgnoreCase) ||
            sanitized.Equals("undefined", System.StringComparison.OrdinalIgnoreCase) ||
            sanitized.Equals("null", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"Detected problematic input '{input}', using fallback '{fallback}'");
            return fallback;
        }
        
        return sanitized;
    }

    /// <summary>
    /// Safely attempts to parse an enum with fallback handling
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="input">String to parse</param>
    /// <param name="fallback">Fallback enum value</param>
    /// <returns>Parsed enum or fallback</returns>
    public static T SafeParseEnum<T>(string input, T fallback) where T : struct
    {
        try
        {
            var sanitized = SanitizeStringInput(input, fallback.ToString());
            var result = (T)System.Enum.Parse(typeof(T), sanitized, true);
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to parse enum '{input}' as {typeof(T).Name}: {ex.Message}");
        }
        
        Debug.LogWarning($"Using fallback value '{fallback}' for enum parsing of '{input}'");
        return fallback;
    }

    public static void SendPacketToServer(byte[] data, P2PPackageHandler.MsgType messageType,
        NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("Attempted to send packet when not connected to server");
            return;
        }

        if (data == null)
        {
            Debug.LogWarning("Attempted to send null data, using empty array");
            data = EmptyByteArray;
        }

        try
        {
            var msg = LidgrenData.LocalClient.CreateMessage();
            msg.Write(SteamUtils.GetServerRealTime()); // time sent
            msg.Write((byte)messageType); // packet type
            msg.Write(data); // packet data

            LidgrenData.LocalClient.SendMessage(msg, LidgrenData.ServerConnection, sendMethod, channel);
            PacketsSent++;

            Debug.Log($"Sent packet - Type: {messageType}, Size: {data.Length} bytes, Method: {sendMethod}, Channel: {channel}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to send packet: {ex.Message}");
            LastError = $"Send packet failed: {ex.Message}";
            
            // Try to handle common issues
            if (ex.Message.Contains("disposed") || ex.Message.Contains("shutdown"))
            {
                Debug.LogWarning("Network client appears to be disposed, resetting connection state");
                IsConnecting = false;
                ConnectionTime = 0f;
            }
        }
    }

    public static void ExitServer(bool usingDebugExitButton)
    {
        try
        {
            Debug.Log("Exiting server...");

            // Safely disconnect from server
            if (LidgrenData?.LocalClient != null)
            {
                try
                {
                    if (IsConnected)
                    {
                        LidgrenData.LocalClient.Disconnect("Player disconnected");
                        Debug.Log("Disconnected from server");
                    }
                    
                    // Shutdown the client to release resources
                    LidgrenData.LocalClient.Shutdown("Client shutdown");
                    Debug.Log("Client shutdown completed");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error during client disconnect/shutdown: {ex.Message}");
                }
            }

            // Cancel auth ticket safely
            if (LidgrenData?.AuthTicketHandler != null)
            {
                try
                {
                    SteamUser.CancelAuthTicket(LidgrenData.AuthTicketHandler);
                    Debug.Log("Auth ticket canceled");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error canceling auth ticket: {ex.Message}");
                }
            }

            // Reset connection state
            IsConnecting = false;
            ConnectionTime = 0f;
            
            // Clear LidgrenData reference to prevent stale data usage
            LidgrenData = null;

            if (!usingDebugExitButton) return; // If using the default "Main Menu" button

            // Clean up game state
            try
            {
                var multiplayerManager = GameManager.Instance?.mMultiplayerManager;
                if (multiplayerManager != null)
                {
                    multiplayerManager.OnDisconnected(); // Removes player objects from screen
                    Debug.Log("Multiplayer manager cleaned up");
                }

                var gameManager = GameManager.Instance;
                if (gameManager != null)
                {
                    gameManager.RestartGame(); // Sends player back to main menu
                    Debug.Log("Game restarted to main menu");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during game state cleanup: {ex.Message}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during server exit: {ex.Message}");
            LastError = $"Exit failed: {ex.Message}";
            
            // Force reset connection state even if cleanup failed
            IsConnecting = false;
            ConnectionTime = 0f;
            LidgrenData = null;
        }
    }

    public static void SetConnecting(bool connecting)
    {
        IsConnecting = connecting;
        if (connecting)
        {
            ConnectionTime = Time.time;
            LastError = null;
        }
    }

    public static void IncrementPacketsReceived()
    {
        PacketsReceived++;
    }

    public static void SetError(string error)
    {
        LastError = error;
        Debug.LogError($"Network error: {error}");
    }

    public static string GetConnectionInfo()
    {
        if (LidgrenData?.ServerConnection == null)
            return "No connection data available";

        var connection = LidgrenData.ServerConnection;
        var info = $"Status: {connection.Status}\n";

        if (connection.Status == NetConnectionStatus.Connected)
        {
            info += $"Remote: {connection.RemoteEndPoint}\n";
            info += $"Ping: {connection.AverageRoundtripTime * 1000:F0}ms\n";
            info += $"Sent: {PacketsSent} packets\n";
            info += $"Received: {PacketsReceived} packets";
        }

        return info;
    }

    public static void ResetStatistics()
    {
        PacketsSent = 0;
        PacketsReceived = 0;
        ConnectionTime = 0f;
        LastError = null;
    }
}
