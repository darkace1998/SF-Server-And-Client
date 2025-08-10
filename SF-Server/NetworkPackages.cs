
using System;
using System.Linq;
namespace SFServer;

/// <summary>
/// Represents a position update package for network transmission.
/// </summary>
public struct PositionPackage : IEquatable<PositionPackage>
{
    private readonly Vector3 _position;
    private readonly Vector2 _rotation;
    private readonly sbyte _yValue;
    private readonly byte _movementType;

    /// <summary>
    /// Gets the position vector.
    /// </summary>
    public Vector3 Position => _position;

    /// <summary>
    /// Gets the rotation vector.
    /// </summary>
    public Vector2 Rotation => _rotation;

    /// <summary>
    /// Gets the Y value.
    /// </summary>
    public sbyte YValue => _yValue;

    /// <summary>
    /// Gets the movement type.
    /// </summary>
    public byte MovementType => _movementType;

    /// <summary>
    /// The byte size of the package.
    /// </summary>
    public static int ByteSize => 11;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionPackage"/> struct.
    /// </summary>
    public PositionPackage(Vector3 position, Vector2 rotation, sbyte yValue, byte movementType)
    {
        _position = position;
        _rotation = rotation;
        _yValue = yValue;
        _movementType = movementType;
    }

    /// <summary>
    /// Returns a string representation of the package.
    /// </summary>
    public override string ToString()
        => $"Position: {Position} Rotation: {Rotation}\nYValue: {YValue}\nMovementType: {MovementType}";

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="PositionPackage"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is PositionPackage other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="PositionPackage"/> is equal to the current <see cref="PositionPackage"/>.
    /// </summary>
    public bool Equals(PositionPackage other)
        => _position.Equals(other._position) && _rotation.Equals(other._rotation) && _yValue == other._yValue && _movementType == other._movementType;

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_position, _rotation, _yValue, _movementType);

    /// <summary>
    /// Checks equality between two <see cref="PositionPackage"/> instances.
    /// </summary>
    public static bool operator ==(PositionPackage left, PositionPackage right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="PositionPackage"/> instances.
    /// </summary>
    public static bool operator !=(PositionPackage left, PositionPackage right) => !(left == right);
}

/// <summary>
/// Represents a weapon update package for network transmission.
/// </summary>
public struct WeaponPackage : IEquatable<WeaponPackage>
{
    private readonly byte _weaponType;
    private readonly byte _fightState;
    private readonly ProjectilePackage[] _projectilePackages;

    /// <summary>
    /// Gets the weapon type.
    /// </summary>
    public byte WeaponType => _weaponType;

    /// <summary>
    /// Gets the fight state.
    /// </summary>
    public byte FightState => _fightState;


    /// <summary>
    /// Returns a copy of the projectile packages array.
    /// </summary>
    public ProjectilePackage[] GetProjectilePackages() => _projectilePackages != null ? (ProjectilePackage[])_projectilePackages.Clone() : Array.Empty<ProjectilePackage>();

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaponPackage"/> struct.
    /// </summary>
    public WeaponPackage(byte weaponType, byte fightState, ProjectilePackage[] projectilePackages)
    {
        _weaponType = weaponType;
        _fightState = fightState;
        _projectilePackages = projectilePackages != null ? (ProjectilePackage[])projectilePackages.Clone() : Array.Empty<ProjectilePackage>();
    }

    /// <summary>
    /// Returns a string representation of the package.
    /// </summary>
    public override string ToString() => $"WeaponType: {WeaponType} FightState: {FightState}";

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="WeaponPackage"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is WeaponPackage other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="WeaponPackage"/> is equal to the current <see cref="WeaponPackage"/>.
    /// </summary>
    public bool Equals(WeaponPackage other)
        => _weaponType == other._weaponType && _fightState == other._fightState && _projectilePackages.SequenceEqual(other._projectilePackages);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_weaponType, _fightState, _projectilePackages != null ? string.Join("|", _projectilePackages.Select(p => p.GetHashCode())) : "");

    /// <summary>
    /// Checks equality between two <see cref="WeaponPackage"/> instances.
    /// </summary>
    public static bool operator ==(WeaponPackage left, WeaponPackage right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="WeaponPackage"/> instances.
    /// </summary>
    public static bool operator !=(WeaponPackage left, WeaponPackage right) => !(left == right);
}

/// <summary>
/// Represents a projectile update package for network transmission.
/// </summary>
public struct ProjectilePackage : IEquatable<ProjectilePackage>
{
    private readonly Vector2 _shootPosition;
    private readonly Vector2 _shootVector;
    private readonly ushort _syncIndex;

    /// <summary>
    /// Gets the shoot position.
    /// </summary>
    public Vector2 ShootPosition => _shootPosition;

    /// <summary>
    /// Gets the shoot vector.
    /// </summary>
    public Vector2 ShootVector => _shootVector;

    /// <summary>
    /// Gets the sync index.
    /// </summary>
    public ushort SyncIndex => _syncIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectilePackage"/> struct.
    /// </summary>
    public ProjectilePackage(Vector2 shootPosition, Vector2 shootVector, ushort syncIndex)
    {
        _shootPosition = shootPosition;
        _shootVector = shootVector;
        _syncIndex = syncIndex;
    }

    /// <summary>
    /// The byte size of the package.
    /// </summary>
    public static int ByteSize => 8;

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="ProjectilePackage"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is ProjectilePackage other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="ProjectilePackage"/> is equal to the current <see cref="ProjectilePackage"/>.
    /// </summary>
    public bool Equals(ProjectilePackage other)
        => _shootPosition.Equals(other._shootPosition) && _shootVector.Equals(other._shootVector) && _syncIndex == other._syncIndex;

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_shootPosition, _shootVector, _syncIndex);

    /// <summary>
    /// Checks equality between two <see cref="ProjectilePackage"/> instances.
    /// </summary>
    public static bool operator ==(ProjectilePackage left, ProjectilePackage right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="ProjectilePackage"/> instances.
    /// </summary>
    public static bool operator !=(ProjectilePackage left, ProjectilePackage right) => !(left == right);
}

/// <summary>
/// Represents movement states for a player.
/// </summary>
[Flags]
public enum MovementState
{
    /// <summary>
    /// No movement.
    /// </summary>
    None = 0,
    /// <summary>
    /// Moving left.
    /// </summary>
    Left = 1,
    /// <summary>
    /// Moving right.
    /// </summary>
    Right = 2,
    /// <summary>
    /// Wall jump.
    /// </summary>
    WallJump = 4,
    /// <summary>
    /// Ground jump.
    /// </summary>
    GroundJump = 8
}

/// <summary>
/// Represents player statistics for network transmission.
/// Contains 13 statistical values as used in the game protocol.
/// </summary>
public struct PlayerStats : IEquatable<PlayerStats>
{
    private readonly int _kills;
    private readonly int _deaths;
    private readonly int _wins;
    private readonly int _gamesPlayed;
    private readonly float _totalDamageDealt;
    private readonly float _totalDamageTaken;
    private readonly int _weaponsPickedUp;
    private readonly int _projectilesFired;
    private readonly int _projectilesHit;
    private readonly float _survivalTime;
    private readonly int _fallOuts;
    private readonly int _suicides;
    private readonly int _disconnects;

    /// <summary>
    /// Gets the number of kills.
    /// </summary>
    public int Kills => _kills;

    /// <summary>
    /// Gets the number of deaths.
    /// </summary>
    public int Deaths => _deaths;

    /// <summary>
    /// Gets the number of wins.
    /// </summary>
    public int Wins => _wins;

    /// <summary>
    /// Gets the number of games played.
    /// </summary>
    public int GamesPlayed => _gamesPlayed;

    /// <summary>
    /// Gets the total damage dealt.
    /// </summary>
    public float TotalDamageDealt => _totalDamageDealt;

    /// <summary>
    /// Gets the total damage taken.
    /// </summary>
    public float TotalDamageTaken => _totalDamageTaken;

    /// <summary>
    /// Gets the number of weapons picked up.
    /// </summary>
    public int WeaponsPickedUp => _weaponsPickedUp;

    /// <summary>
    /// Gets the number of projectiles fired.
    /// </summary>
    public int ProjectilesFired => _projectilesFired;

    /// <summary>
    /// Gets the number of projectiles that hit.
    /// </summary>
    public int ProjectilesHit => _projectilesHit;

    /// <summary>
    /// Gets the total survival time.
    /// </summary>
    public float SurvivalTime => _survivalTime;

    /// <summary>
    /// Gets the number of fall outs.
    /// </summary>
    public int FallOuts => _fallOuts;

    /// <summary>
    /// Gets the number of suicides.
    /// </summary>
    public int Suicides => _suicides;

    /// <summary>
    /// Gets the number of disconnects.
    /// </summary>
    public int Disconnects => _disconnects;

    /// <summary>
    /// The byte size of the stats package (13 integers = 52 bytes).
    /// </summary>
    public static int ByteSize => 52;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerStats"/> struct.
    /// </summary>
    public PlayerStats(int kills = 0, int deaths = 0, int wins = 0, int gamesPlayed = 0,
        float totalDamageDealt = 0f, float totalDamageTaken = 0f, int weaponsPickedUp = 0,
        int projectilesFired = 0, int projectilesHit = 0, float survivalTime = 0f,
        int fallOuts = 0, int suicides = 0, int disconnects = 0)
    {
        _kills = kills;
        _deaths = deaths;
        _wins = wins;
        _gamesPlayed = gamesPlayed;
        _totalDamageDealt = totalDamageDealt;
        _totalDamageTaken = totalDamageTaken;
        _weaponsPickedUp = weaponsPickedUp;
        _projectilesFired = projectilesFired;
        _projectilesHit = projectilesHit;
        _survivalTime = survivalTime;
        _fallOuts = fallOuts;
        _suicides = suicides;
        _disconnects = disconnects;
    }

    /// <summary>
    /// Creates a default (empty) stats instance.
    /// </summary>
    public static PlayerStats Default => new();

    /// <summary>
    /// Converts the stats to a byte array for network transmission.
    /// </summary>
    public byte[] ToByteArray()
    {
        var bytes = new byte[ByteSize];
        var index = 0;

        // Pack all stats as integers (13 total)
        BitConverter.GetBytes(_kills).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_deaths).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_wins).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_gamesPlayed).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_totalDamageDealt).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_totalDamageTaken).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_weaponsPickedUp).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_projectilesFired).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_projectilesHit).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_survivalTime).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_fallOuts).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_suicides).CopyTo(bytes, index); index += 4;
        BitConverter.GetBytes(_disconnects).CopyTo(bytes, index);

        return bytes;
    }

    /// <summary>
    /// Returns a string representation of the stats.
    /// </summary>
    public override string ToString()
        => $"Kills: {Kills}, Deaths: {Deaths}, Wins: {Wins}, Games: {GamesPlayed}";

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="PlayerStats"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is PlayerStats other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="PlayerStats"/> is equal to the current <see cref="PlayerStats"/>.
    /// </summary>
    public bool Equals(PlayerStats other)
        => _kills == other._kills && _deaths == other._deaths && _wins == other._wins &&
           _gamesPlayed == other._gamesPlayed && _totalDamageDealt.Equals(other._totalDamageDealt) &&
           _totalDamageTaken.Equals(other._totalDamageTaken) && _weaponsPickedUp == other._weaponsPickedUp &&
           _projectilesFired == other._projectilesFired && _projectilesHit == other._projectilesHit &&
           _survivalTime.Equals(other._survivalTime) && _fallOuts == other._fallOuts &&
           _suicides == other._suicides && _disconnects == other._disconnects;

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_kills);
        hash.Add(_deaths);
        hash.Add(_wins);
        hash.Add(_gamesPlayed);
        hash.Add(_totalDamageDealt);
        hash.Add(_totalDamageTaken);
        hash.Add(_weaponsPickedUp);
        hash.Add(_projectilesFired);
        hash.Add(_projectilesHit);
        hash.Add(_survivalTime);
        hash.Add(_fallOuts);
        hash.Add(_suicides);
        hash.Add(_disconnects);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Checks equality between two <see cref="PlayerStats"/> instances.
    /// </summary>
    public static bool operator ==(PlayerStats left, PlayerStats right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="PlayerStats"/> instances.
    /// </summary>
    public static bool operator !=(PlayerStats left, PlayerStats right) => !(left == right);
}

/// <summary>
/// Represents network game options for server configuration.
/// </summary>
public struct NetworkOptions : IEquatable<NetworkOptions>
{
    private readonly byte _mapOption;
    private readonly byte _hpOption;
    private readonly byte _regenOption;
    private readonly byte _weaponsSpawnOption;

    /// <summary>
    /// Gets the map option setting.
    /// </summary>
    public byte MapOption => _mapOption;

    /// <summary>
    /// Gets the HP option setting.
    /// </summary>
    public byte HpOption => _hpOption;

    /// <summary>
    /// Gets the regeneration option setting.
    /// </summary>
    public byte RegenOption => _regenOption;

    /// <summary>
    /// Gets the weapons spawn option setting.
    /// </summary>
    public byte WeaponsSpawnOption => _weaponsSpawnOption;

    /// <summary>
    /// The byte size of the network options (4 bytes).
    /// </summary>
    public static int ByteSize => 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkOptions"/> struct.
    /// </summary>
    public NetworkOptions(byte mapOption = 0, byte hpOption = 0, byte regenOption = 0, byte weaponsSpawnOption = 0)
    {
        _mapOption = mapOption;
        _hpOption = hpOption;
        _regenOption = regenOption;
        _weaponsSpawnOption = weaponsSpawnOption;
    }

    /// <summary>
    /// Creates default network options.
    /// </summary>
    public static NetworkOptions Default => new();

    /// <summary>
    /// Creates network options with all features enabled.
    /// </summary>
    public static NetworkOptions AllEnabled => new(1, 1, 1, 1);

    /// <summary>
    /// Converts the options to a byte array for network transmission.
    /// </summary>
    public byte[] ToByteArray() => new[] { _mapOption, _hpOption, _regenOption, _weaponsSpawnOption };

    /// <summary>
    /// Returns a string representation of the network options.
    /// </summary>
    public override string ToString()
        => $"Maps: {MapOption}, HP: {HpOption}, Regen: {RegenOption}, Weapons: {WeaponsSpawnOption}";

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="NetworkOptions"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is NetworkOptions other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="NetworkOptions"/> is equal to the current <see cref="NetworkOptions"/>.
    /// </summary>
    public bool Equals(NetworkOptions other)
        => _mapOption == other._mapOption && _hpOption == other._hpOption &&
           _regenOption == other._regenOption && _weaponsSpawnOption == other._weaponsSpawnOption;

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_mapOption, _hpOption, _regenOption, _weaponsSpawnOption);

    /// <summary>
    /// Checks equality between two <see cref="NetworkOptions"/> instances.
    /// </summary>
    public static bool operator ==(NetworkOptions left, NetworkOptions right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="NetworkOptions"/> instances.
    /// </summary>
    public static bool operator !=(NetworkOptions left, NetworkOptions right) => !(left == right);
}
