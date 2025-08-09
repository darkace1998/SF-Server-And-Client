using Lidgren.Network;
using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public static class NetworkUtils
{
    public static LidgrenData LidgrenData;

    // TODO: Switch array below for List<byte[]> later for better flexibility?
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

    public static void SendPacketToServer(byte[] data, P2PPackageHandler.MsgType messageType,
        NetDeliveryMethod sendMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("Attempted to send packet when not connected to server");
            return;
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
        }
    }

    public static void ExitServer(bool usingDebugExitButton)
    {
        try
        {
            Debug.Log("Exiting server...");

            if (LidgrenData?.LocalClient != null && IsConnected)
            {
                LidgrenData.LocalClient.Disconnect("Player disconnected");
                Debug.Log("Disconnected from server");
            }

            if (LidgrenData?.AuthTicketHandler != null)
            {
                SteamUser.CancelAuthTicket(LidgrenData.AuthTicketHandler);
                Debug.Log("Auth ticket canceled");
            }

            // Reset connection state
            IsConnecting = false;
            ConnectionTime = 0f;

            if (!usingDebugExitButton) return; // If using the default "Main Menu" button

            // Clean up game state
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
            Debug.LogError($"Error during server exit: {ex.Message}");
            LastError = $"Exit failed: {ex.Message}";
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
