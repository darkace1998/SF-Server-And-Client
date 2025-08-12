using Steamworks;
using UnityEngine;
using Lidgren.Network;

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
    private bool _stylesInitialized = false;

    private void Start()
    {
        Debug.Log("Started SF_Lidgren GUI Manager!");
        
        // Initialize safe defaults to prevent undefined behavior
        try
        {
            // Ensure proper framerate handling
            if (Application.targetFrameRate <= 0)
            {
                Application.targetFrameRate = 60; // Set reasonable default
                Debug.Log("Set default framerate to 60 FPS");
            }
            
            // Initialize connection defaults
            if (string.IsNullOrEmpty(Address))
            {
                Address = "localhost";
            }
            
            if (Port <= 0)
            {
                Port = 1337;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during TempGUI Start initialization: {ex.Message}");
        }
    }

    private void InitializeStyles()
    {
        if (_stylesInitialized || GUI.skin == null)
            return;

        try
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

            _stylesInitialized = true;
            Debug.Log("SF_Lidgren GUI styles initialized successfully!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize SF_Lidgren GUI styles: {ex.Message}");
        }
    }

    private void Update()
    {
        try
        {
            // Toggle menu with F1 key
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _showMenu = !_showMenu;
                Debug.Log($"SF_Lidgren menu toggled: {_showMenu}");
            }

            // Show menu by default on first load
            if (!_showMenu && Time.time < 5f)
            {
                _showMenu = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SF_Lidgren Update error: {ex.Message}");
        }
    }

    public void OnGUI()
    {
        try
        {
            // Initialize styles on first GUI call when GUI.skin is available
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            if (!_showMenu)
            {
                // Show minimal toggle hint
                GUI.Label(new Rect(10, 10, 200, 20), "Press F1 to open server menu");
                return;
            }

            _menuRect = GUILayout.Window(1169, _menuRect, DrawServerWindow, "SF Dedicated Server Client");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SF_Lidgren OnGUI error: {ex.Message}");
            
            // Fallback minimal GUI in case of errors
            if (_showMenu)
            {
                GUI.Window(1169, new Rect(Screen.width / 2f - 150f, Screen.height / 2f - 100f, 300f, 200f), (int windowId) =>
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label("SF Dedicated Server (Error Recovery Mode)");
                    GUILayout.Space(10);
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Address:");
                    Address = GUILayout.TextField(Address ?? "localhost");
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Port:");
                    var portString = GUILayout.TextField(Port.ToString());
                    if (int.TryParse(portString, out var newPort)) Port = newPort;
                    GUILayout.EndHorizontal();
                    
                    if (GUILayout.Button("Connect")) ConnectToServer();
                    if (GUILayout.Button("Disconnect")) DisconnectFromServer();
                    if (GUILayout.Button("Hide (F1)")) _showMenu = false;
                    
                    GUILayout.EndVertical();
                }, "SF Server Client");
            }
        }
    }

    private void DrawServerWindow(int windowId)
    {
        GUILayout.BeginVertical();

        // Use fallback styles if initialization failed
        var headerStyle = _headerStyle ?? GUI.skin.label;
        var buttonStyle = _buttonStyle ?? GUI.skin.button;
        var statusStyle = _statusStyle ?? GUI.skin.label;

        // Header
        GUILayout.Label("Stick Fight Dedicated Server", headerStyle);
        GUILayout.Space(10);

        // Connection Section
        GUILayout.Label("Server Connection:", headerStyle);
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
        else if (!string.IsNullOrEmpty(portString) && !int.TryParse(portString, out _))
        {
            // Reset to last valid port if input is invalid
            Debug.LogWarning($"Invalid port number entered: {portString}");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Connection Buttons
        GUILayout.BeginHorizontal();

        var connectionStatus = GetConnectionStatus();
        var isConnected = connectionStatus.Contains("Connected");

        GUI.enabled = !isConnected;
        if (GUILayout.Button("Connect", buttonStyle, GUILayout.Height(30)))
        {
            ConnectToServer();
        }

        GUI.enabled = isConnected;
        if (GUILayout.Button("Disconnect", buttonStyle, GUILayout.Height(30)))
        {
            DisconnectFromServer();
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Status Section
        GUILayout.Label("Status:", headerStyle);
        
        // Apply status color if possible
        if (statusStyle != null)
        {
            var oldColor = statusStyle.normal.textColor;
            statusStyle.normal.textColor = _statusColor;
            GUILayout.Label(connectionStatus, statusStyle);
            statusStyle.normal.textColor = oldColor;
        }
        else
        {
            GUILayout.Label(connectionStatus);
        }

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            if (statusStyle != null)
            {
                var oldColor = statusStyle.normal.textColor;
                statusStyle.normal.textColor = _statusColor;
                GUILayout.Label(_statusMessage, statusStyle);
                statusStyle.normal.textColor = oldColor;
            }
            else
            {
                GUILayout.Label(_statusMessage);
            }
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
            // Validate address and port before attempting connection
            if (string.IsNullOrEmpty(Address) || Address.Trim().Length == 0)
            {
                SetStatus("Invalid address: Address cannot be empty", Color.red);
                return;
            }

            if (Port <= 0 || Port > 65535)
            {
                SetStatus("Invalid port: Port must be between 1 and 65535", Color.red);
                return;
            }

            Debug.Log($"Attempting to connect to {Address}:{Port}...");
            SetStatus("Connecting...", Color.yellow);
            
            // Check if we're already connected
            if (NetworkUtils.IsConnected)
            {
                SetStatus("Already connected to server", Color.yellow);
                return;
            }
            
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
            if (NetworkUtils.IsConnecting)
            {
                return "Starting connection...";
            }
            return "Not connected";
        }

        var connectionStatus = lidgrenData.ServerConnection.Status;
        var statusText = connectionStatus switch
        {
            NetConnectionStatus.None => "Not connected",
            NetConnectionStatus.InitiatedConnect => "Connecting...",
            NetConnectionStatus.ReceivedInitiation => "Received connection",
            NetConnectionStatus.RespondedAwaitingApproval => "Awaiting server approval",
            NetConnectionStatus.RespondedConnect => "Connected (responding)",
            NetConnectionStatus.Connected => "Connected",
            NetConnectionStatus.Disconnecting => "Disconnecting...",
            NetConnectionStatus.Disconnected => GetDisconnectionReason(lidgrenData.ServerConnection),
            _ => $"Unknown status: {connectionStatus}"
        };

        // Add additional context for connection issues
        if (!string.IsNullOrEmpty(NetworkUtils.LastError))
        {
            statusText += $"\nLast error: {NetworkUtils.LastError}";
        }

        return statusText;
    }

    private string GetDisconnectionReason(NetConnection connection)
    {
        if (connection.RemoteEndPoint != null)
        {
            // Try to get disconnection reason if available
            try
            {
                var reasonField = typeof(NetConnection).GetField("m_disconnectReason", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (reasonField != null)
                {
                    var reason = reasonField.GetValue(connection) as string;
                    if (!string.IsNullOrEmpty(reason))
                    {
                        return $"Disconnected: {reason}";
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not get disconnection reason: {ex.Message}");
            }
        }
        return "Disconnected";
    }

    private void SetStatus(string message, Color color)
    {
        _statusMessage = message;
        _statusColor = color;
    }
}
