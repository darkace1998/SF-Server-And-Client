using Steamworks;
using UnityEngine;

namespace SF_Lidgren;

public class TempGUI : MonoBehaviour
{
    private bool _showMenu;
    private bool _showAdvanced = false;
    private string _statusMessage = "";
    private Color _statusColor = Color.white;

    public static string Address = "localhost";
    public static int Port = 1337;

    private Rect _menuRect = new(Screen.width / 2f - 200f, Screen.height / 2f - 200f, 400f, 300f);
    private Vector2 _scrollPosition = Vector2.zero;

    // UI Styles
    private GUIStyle _headerStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _statusStyle;

    private void Start()
    {
        Debug.Log("Started SF_Lidgren GUI Manager!");
        InitializeStyles();
    }

    private void InitializeStyles()
    {
        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        _statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true
        };
    }

    private void Update()
    {
        // Toggle menu with F1 key
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _showMenu = !_showMenu;
        }

        // Show menu by default on first load
        if (!_showMenu && Time.time < 5f)
        {
            _showMenu = true;
        }
    }

    public void OnGUI()
    {
        if (!_showMenu)
        {
            // Show minimal toggle hint
            GUI.Label(new Rect(10, 10, 200, 20), "Press F1 to open server menu");
            return;
        }

        _menuRect = GUILayout.Window(1169, _menuRect, DrawServerWindow, "SF Dedicated Server Client");
    }

    private void DrawServerWindow(int windowId)
    {
        GUILayout.BeginVertical();

        // Header
        GUILayout.Label("Stick Fight Dedicated Server", _headerStyle);
        GUILayout.Space(10);

        // Connection Section
        GUILayout.Label("Server Connection:", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Address:", GUILayout.Width(60));
        Address = GUILayout.TextField(Address, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Port:", GUILayout.Width(60));
        var portString = GUILayout.TextField(Port.ToString(), GUILayout.ExpandWidth(true));
        if (int.TryParse(portString, out var newPort) && newPort > 0 && newPort <= 65535)
        {
            Port = newPort;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Connection Buttons
        GUILayout.BeginHorizontal();

        var connectionStatus = GetConnectionStatus();
        var isConnected = connectionStatus.Contains("Connected");

        GUI.enabled = !isConnected;
        if (GUILayout.Button("Connect", _buttonStyle, GUILayout.Height(30)))
        {
            ConnectToServer();
        }

        GUI.enabled = isConnected;
        if (GUILayout.Button("Disconnect", _buttonStyle, GUILayout.Height(30)))
        {
            DisconnectFromServer();
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Status Section
        GUILayout.Label("Status:", EditorStyles.boldLabel);
        _statusStyle.normal.textColor = _statusColor;
        GUILayout.Label(connectionStatus, _statusStyle);

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            GUILayout.Label(_statusMessage, _statusStyle);
        }

        GUILayout.Space(10);

        // Advanced Section
        _showAdvanced = GUILayout.Toggle(_showAdvanced, "Show Advanced Info");

        if (_showAdvanced)
        {
            GUILayout.Space(5);
            DrawAdvancedInfo();
        }

        GUILayout.Space(10);

        // Control Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Hide Menu (F1)", GUILayout.Height(25)))
        {
            _showMenu = false;
        }

        if (GUILayout.Button("Refresh Status", GUILayout.Height(25)))
        {
            RefreshStatus();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        // Make window draggable
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    private void DrawAdvancedInfo()
    {
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100));

        var lidgrenData = NetworkUtils.LidgrenData;
        if (lidgrenData != null)
        {
            GUILayout.Label($"Local Client Status: {lidgrenData.LocalClient?.Status ?? NetPeerStatus.NotRunning}");
            GUILayout.Label($"Server Connection: {lidgrenData.ServerConnection?.Status ?? NetConnectionStatus.None}");
            GUILayout.Label($"Connection Count: {lidgrenData.LocalClient?.ConnectionsCount ?? 0}");

            if (lidgrenData.ServerConnection != null)
            {
                GUILayout.Label($"Ping: {lidgrenData.ServerConnection.AverageRoundtripTime * 1000:F0}ms");
                GUILayout.Label($"Remote Address: {lidgrenData.ServerConnection.RemoteEndPoint}");
            }
        }

        GUILayout.Label($"Is Inside Lobby: {MatchmakingHandler.Instance?.IsInsideLobby ?? false}");
        GUILayout.Label($"Is Network Match: {MatchmakingHandler.IsNetworkMatch}");
        GUILayout.Label($"Running on Sockets: {MatchmakingHandler.RunningOnSockets}");

        GUILayout.EndScrollView();
    }

    private void ConnectToServer()
    {
        try
        {
            Debug.Log($"Attempting to connect to {Address}:{Port}...");
            SetStatus("Connecting...", Color.yellow);
            MatchMakingHandlerSockets.Instance.JoinServer();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to connect: {ex.Message}");
            SetStatus($"Connection failed: {ex.Message}", Color.red);
        }
    }

    private void DisconnectFromServer()
    {
        try
        {
            Debug.Log("Disconnecting from server...");
            SetStatus("Disconnecting...", Color.yellow);
            NetworkUtils.ExitServer(true);
            SetStatus("Disconnected", Color.white);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to disconnect: {ex.Message}");
            SetStatus($"Disconnect failed: {ex.Message}", Color.red);
        }
    }

    private void RefreshStatus()
    {
        var status = GetConnectionStatus();
        Debug.Log($"Status refreshed: {status}");
    }

    private string GetConnectionStatus()
    {
        var lidgrenData = NetworkUtils.LidgrenData;
        if (lidgrenData?.ServerConnection == null)
        {
            return "Not connected";
        }

        var connectionStatus = lidgrenData.ServerConnection.Status;
        return connectionStatus switch
        {
            NetConnectionStatus.None => "Not connected",
            NetConnectionStatus.InitiatedConnect => "Connecting...",
            NetConnectionStatus.ReceivedInitiation => "Received connection",
            NetConnectionStatus.RespondedAwaitingApproval => "Awaiting approval",
            NetConnectionStatus.RespondedConnect => "Connected (responding)",
            NetConnectionStatus.Connected => "Connected",
            NetConnectionStatus.Disconnecting => "Disconnecting...",
            NetConnectionStatus.Disconnected => "Disconnected",
            _ => $"Unknown status: {connectionStatus}"
        };
    }

    private void SetStatus(string message, Color color)
    {
        _statusMessage = message;
        _statusColor = color;
    }
}
