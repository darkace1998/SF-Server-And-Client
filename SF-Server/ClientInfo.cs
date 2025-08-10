using System.Net;
using Lidgren.Network;
using SFServer;

namespace SFServer;

/// <summary>
/// Represents information about a connected client.
/// </summary>
public sealed class ClientInfo : IEquatable<ClientInfo>
{
    /// <summary>
    /// Gets the SteamID of the client.
    /// </summary>
    public SteamId SteamID { get; }

    /// <summary>
    /// Gets the username of the client.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets or sets the IP address of the client.
    /// </summary>
    public IPAddress Address { get; set; }

    /// <summary>
    /// Gets or sets the connection status of the client.
    /// </summary>
    public NetConnectionStatus Status { get; set; }

    /// <summary>
    /// Gets the player index of the client.
    /// </summary>
    public int PlayerIndex { get; }

    /// <summary>
    /// Gets or sets the ping value of the client.
    /// </summary>
    public int Ping { get; set; }

    /// <summary>
    /// Gets or sets the position information of the client.
    /// </summary>
    public PositionPackage PositionInfo { get; set; }

    /// <summary>
    /// Gets or sets the weapon information of the client.
    /// </summary>
    public WeaponPackage WeaponInfo { get; set; }

    /// <summary>
    /// Gets the current HP of the client.
    /// </summary>
    public float Hp { get; private set; }

    /// <summary>
    /// Gets whether the client is alive.
    /// </summary>
    public bool IsAlive { get; private set; }

    /// <summary>
    /// Gets the authentication ticket of the client.
    /// </summary>
    public AuthTicket AuthTicket { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientInfo"/> class.
    /// </summary>
    /// <param name="steamID">The SteamID of the client.</param>
    /// <param name="steamUsername">The username of the client.</param>
    /// <param name="authTicket">The authentication ticket.</param>
    /// <param name="address">The IP address of the client.</param>
    /// <param name="playerIndex">The player index.</param>
    public ClientInfo(SteamId steamID, string steamUsername, AuthTicket authTicket, IPAddress address, int playerIndex)
    {
        SteamID = steamID;
        Username = steamUsername;
        AuthTicket = authTicket;
        Address = address;
        PlayerIndex = playerIndex;
        Ping = 0;
        Hp = 100;
        IsAlive = true;
        PositionInfo = new PositionPackage();
    }

    /// <summary>
    /// Deducts HP from the client and sets IsAlive to false if HP falls below or equals zero.
    /// </summary>
    /// <param name="amount">The amount of HP to deduct.</param>
    public void DeductHp(float amount)
    {
        Hp -= amount;

        if (Hp <= 0)
            IsAlive = false;
    }

    /// <summary>
    /// Revives the client and restores HP to 100.
    /// </summary>
    public void Revive()
    {
        IsAlive = true;
        Hp = 100;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="ClientInfo"/>.
    /// </summary>
    public override bool Equals(object obj) => obj is ClientInfo client && Equals(client.Address, Address);

    /// <summary>
    /// Determines whether the specified <see cref="ClientInfo"/> is equal to the current <see cref="ClientInfo"/>.
    /// </summary>
    public bool Equals(ClientInfo other) => other is not null && Equals(other.Address, Address);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => Address.GetHashCode();

    /// <summary>
    /// Returns a string representation of the client info.
    /// </summary>
    public override string ToString()
        => $"\nSteamID: {SteamID}\nName: {Username}\nAddress: {Address}\nAuthTicket: {AuthTicket.ToString().Truncate(10)}"
           + $"\nPlayerIndex: {PlayerIndex}\nPing: {Ping}";
}
