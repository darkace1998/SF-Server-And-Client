using System;
using System.Security.Cryptography;
using System.Linq;
namespace SFServer;

/// <summary>
/// Manages game maps and map transitions
/// </summary>
public class MapManager
{
    private readonly RandomNumberGenerator _rng;
    // private readonly ServerConfig _config; // Removed unused field
    private int _currentMapId;
    private MapType _currentMapType;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapManager"/> class.
    /// </summary>
    /// <param name="config">The server configuration.</param>
    public MapManager(ServerConfig config)
    {
    // _config = config; // Removed unused field
        _rng = RandomNumberGenerator.Create();
        _currentMapId = 0; // Start with lobby map
        _currentMapType = MapType.Lobby;
    }

    /// <summary>
    /// Gets the current map ID.
    /// </summary>
    public int CurrentMapId => _currentMapId;

    /// <summary>
    /// Gets the current map type.
    /// </summary>
    public MapType CurrentMapType => _currentMapType;

    /// <summary>
    /// Gets the next map for the round.
    /// </summary>
    /// <returns>Map data containing type and ID.</returns>
    public MapData GetNextMap()
    {
        // For now, use random selection from basic maps
        // Custom map support and rotation lists can be added here
        var bytes = new byte[4];
        _rng.GetBytes(bytes);
        var mapId = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 110; // Basic game maps range
        _currentMapId = mapId;
        _currentMapType = MapType.Standard;

        return new MapData(_currentMapType, mapId, BitConverter.GetBytes(mapId));
    }

    /// <summary>
    /// Gets the lobby map data.
    /// </summary>
    public MapData LobbyMap
    {
        get
        {
            _currentMapId = 0;
            _currentMapType = MapType.Lobby;
            return new MapData(MapType.Lobby, 0, new byte[] { 0, 0, 0, 0 });
        }
    }

    /// <summary>
    /// Gets the lobby map data.
    /// </summary>
    /// <returns>Map data for the lobby.</returns>
    public MapData GetLobbyMap()
    {
        _currentMapId = 0;
        _currentMapType = MapType.Lobby;
        return new MapData(MapType.Lobby, 0, new byte[] { 0, 0, 0, 0 });
    }

    /// <summary>
    /// Validates if a map change request is valid.
    /// </summary>
    /// <param name="requesterIndex">Player requesting the change.</param>
    /// <param name="mapData">Requested map data.</param>
    /// <returns>True if valid.</returns>
    public static bool ValidateMapChange(int requesterIndex, byte[] mapData)
    {
        // Basic validation: must be at least 4 bytes
        if (mapData == null || mapData.Length < 4)
            return false;
        return true;
    }

    /// <summary>
    /// Processes a map change request.
    /// </summary>
    /// <param name="mapData">New map data.</param>
    public void ProcessMapChange(byte[] mapData)
    {
    ArgumentNullException.ThrowIfNull(mapData);
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
public struct MapData : IEquatable<MapData>
{
    private readonly MapType _mapType;
    private readonly int _mapId;
    private readonly byte[] _data;

    /// <summary>
    /// Gets the map type.
    /// </summary>
    public MapType MapType => _mapType;

    /// <summary>
    /// Gets the map ID.
    /// </summary>
    public int MapId => _mapId;

    /// <summary>
    /// Returns a copy of the map data bytes.
    /// </summary>
    public byte[] GetData() => _data != null ? (byte[])_data.Clone() : Array.Empty<byte>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MapData"/> struct.
    /// </summary>
    public MapData(MapType mapType, int mapId, byte[] data)
    {
        _mapType = mapType;
        _mapId = mapId;
        _data = data != null ? (byte[])data.Clone() : Array.Empty<byte>();
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="MapData"/>.
    /// </summary>
    public override bool Equals(object obj)
        => obj is MapData other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="MapData"/> is equal to the current <see cref="MapData"/>.
    /// </summary>
    public bool Equals(MapData other)
        => _mapType == other._mapType && _mapId == other._mapId && _data.SequenceEqual(other._data);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode()
        => HashCode.Combine(_mapType, _mapId, _data != null ? BitConverter.ToString(_data) : "");

    /// <summary>
    /// Checks equality between two <see cref="MapData"/> instances.
    /// </summary>
    public static bool operator ==(MapData left, MapData right)
        => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="MapData"/> instances.
    /// </summary>
    public static bool operator !=(MapData left, MapData right)
        => !(left == right);
}

/// <summary>
/// Types of maps.
/// </summary>
public enum MapType
{
    /// <summary>
    /// The lobby map.
    /// </summary>
    Lobby = 0,
    /// <summary>
    /// Standard game map.
    /// </summary>
    Standard = 1,
    /// <summary>
    /// Workshop map.
    /// </summary>
    Workshop = 2,
    /// <summary>
    /// Custom map.
    /// </summary>
    Custom = 3
}
