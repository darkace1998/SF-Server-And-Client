namespace SF_Server;

public struct PositionPackage : IEquatable<PositionPackage>
{
    public Vector3 Position;
    public Vector2 Rotation;
    public sbyte YValue;
    public byte MovementType;

    public static int ByteSize => 11;

    public override string ToString()
        => $"Position: {Position} Rotation: {Rotation}\nYValue: {YValue}\nMovementType: {MovementType}";

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(PositionPackage left, PositionPackage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PositionPackage left, PositionPackage right)
    {
        return !(left == right);
    }

    public bool Equals(PositionPackage other)
    {
        throw new NotImplementedException();
    }
}

public struct WeaponPackage : IEquatable<WeaponPackage>
{
    public byte WeaponType;
    public byte FightState;

    public ProjectilePackage[] ProjectilePackages;

    public override string ToString() => $"WeaponType: {WeaponType} FightState: {FightState}";

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(WeaponPackage left, WeaponPackage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WeaponPackage left, WeaponPackage right)
    {
        return !(left == right);
    }

    public bool Equals(WeaponPackage other)
    {
        throw new NotImplementedException();
    }
}

public struct ProjectilePackage : IEquatable<ProjectilePackage>
{
    public Vector2 ShootPosition;
    public Vector2 ShootVector;
    public ushort SyncIndex;

    public ProjectilePackage(Vector2 shootPosition, Vector2 shootVector, ushort syncIndex)
    {
        ShootPosition = shootPosition;
        ShootVector = shootVector;
        SyncIndex = syncIndex;
    }

    public static int ByteSize => 8;

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(ProjectilePackage left, ProjectilePackage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProjectilePackage left, ProjectilePackage right)
    {
        return !(left == right);
    }

    public bool Equals(ProjectilePackage other)
    {
        throw new NotImplementedException();
    }
}

[Flags]
public enum MovementStateEnum
{
    None = 0,
    Left = 1,
    Right = 2,
    WallJump = 4,
    GroundJump = 8
}
