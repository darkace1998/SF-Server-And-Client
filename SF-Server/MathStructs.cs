
namespace SFServer;

/// <summary>
/// Represents a 2D vector.
/// </summary>
public struct Vector2 : IEquatable<Vector2>
{
    private readonly float _x;
    private readonly float _y;

    /// <summary>
    /// Gets the X component.
    /// </summary>
    public float X => _x;

    /// <summary>
    /// Gets the Y component.
    /// </summary>
    public float Y => _y;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector2"/> struct.
    /// </summary>
    public Vector2(float x, float y)
    {
        _x = x;
        _y = y;
    }

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    public override string ToString() => $"[X: {X}, Y: {Y}]";

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="Vector2"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is Vector2 other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="Vector2"/> is equal to the current <see cref="Vector2"/>.
    /// </summary>
    /// <summary>
    /// Checks equality with a tolerance for floating-point values.
    /// </summary>
    public bool Equals(Vector2 other)
    {
        const float epsilon = 1e-6f;
        return Math.Abs(_x - other._x) < epsilon && Math.Abs(_y - other._y) < epsilon;
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_x, _y);

    /// <summary>
    /// Checks equality between two <see cref="Vector2"/> instances.
    /// </summary>
    public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="Vector2"/> instances.
    /// </summary>
    public static bool operator !=(Vector2 left, Vector2 right) => !(left == right);
}

/// <summary>
/// Represents a 3D vector.
/// </summary>
public struct Vector3 : IEquatable<Vector3>
{
    private readonly float _x;
    private readonly float _y;
    private readonly float _z;

    /// <summary>
    /// Gets the X component.
    /// </summary>
    public float X => _x;

    /// <summary>
    /// Gets the Y component.
    /// </summary>
    public float Y => _y;

    /// <summary>
    /// Gets the Z component.
    /// </summary>
    public float Z => _z;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector3"/> struct.
    /// </summary>
    public Vector3(float x, float y, float z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    public override string ToString() => $"[X: {X}, Y: {Y} Z: {Z}]";

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="Vector3"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is Vector3 other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="Vector3"/> is equal to the current <see cref="Vector3"/>.
    /// </summary>
    /// <summary>
    /// Checks equality with a tolerance for floating-point values.
    /// </summary>
    public bool Equals(Vector3 other)
    {
        const float epsilon = 1e-6f;
        return Math.Abs(_x - other._x) < epsilon && Math.Abs(_y - other._y) < epsilon && Math.Abs(_z - other._z) < epsilon;
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_x, _y, _z);

    /// <summary>
    /// Checks equality between two <see cref="Vector3"/> instances.
    /// </summary>
    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two <see cref="Vector3"/> instances.
    /// </summary>
    public static bool operator !=(Vector3 left, Vector3 right) => !(left == right);
}
