
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
