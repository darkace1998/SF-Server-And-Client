namespace SF_Server;

/// <summary>
/// Manages game maps and map transitions
/// </summary>
public class MapManager
{
    private readonly Random _random;
    private readonly ServerConfig _config;
    private int _currentMapId;
    private MapType _currentMapType;

    public MapManager(ServerConfig config)
    {
        _config = config;
        _random = new Random();
        _currentMapId = 0; // Start with lobby map
        _currentMapType = MapType.Lobby;
    }

    public int CurrentMapId => _currentMapId;
    public MapType CurrentMapType => _currentMapType;

    /// <summary>
    /// Get the next map for the round
    /// </summary>
    /// <returns>Map data containing type and ID</returns>
    public MapData GetNextMap()
    {
        // For now, use random selection from basic maps
        // TODO: Implement custom map support and rotation lists
        
        var mapId = _random.Next(0, 110); // Basic game maps range
        _currentMapId = mapId;
        _currentMapType = MapType.Standard;
        
        return new MapData
        {
            MapType = _currentMapType,
            MapId = mapId,
            Data = BitConverter.GetBytes(mapId) // Simple int conversion for basic maps
        };
    }

    /// <summary>
    /// Switch to lobby map
    /// </summary>
    /// <returns>Lobby map data</returns>
    public MapData GetLobbyMap()
    {
        _currentMapId = 0;
        _currentMapType = MapType.Lobby;
        
        return new MapData
        {
            MapType = MapType.Lobby,
            MapId = 0,
            Data = new byte[] { 0, 0, 0, 0 } // Lobby map data
        };
    }

    /// <summary>
    /// Validate if a map change request is valid
    /// </summary>
    /// <param name="requesterIndex">Player requesting the change</param>
    /// <param name="mapData">Requested map data</param>
    /// <returns>True if valid</returns>
    public bool ValidateMapChange(int requesterIndex, byte[] mapData)
    {
        // TODO: Implement proper validation logic
        // For now, allow any map change
        
        if (mapData == null || mapData.Length < 4)
            return false;
            
        return true;
    }

    /// <summary>
    /// Process a map change request
    /// </summary>
    /// <param name="mapData">New map data</param>
    public void ProcessMapChange(byte[] mapData)
    {
        if (mapData.Length >= 4)
        {
            _currentMapId = BitConverter.ToInt32(mapData, 0);
            _currentMapType = _currentMapId == 0 ? MapType.Lobby : MapType.Standard;
        }
    }
}

/// <summary>
/// Map data structure
/// </summary>
public struct MapData
{
    public MapType MapType;
    public int MapId;
    public byte[] Data; // Renamed from MapData to Data
}

/// <summary>
/// Types of maps
/// </summary>
public enum MapType : byte
{
    Lobby = 0,
    Standard = 1,
    Workshop = 2,
    Custom = 3
}