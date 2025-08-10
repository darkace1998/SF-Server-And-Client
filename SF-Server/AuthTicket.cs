using System.Text;
using System;

namespace SFServer;

/// <summary>
/// Represents an authentication ticket for Steamworks.
/// </summary>
public readonly struct AuthTicket : IEquatable<AuthTicket>
{

    private readonly byte[] _ticket;
    private readonly string _ticketString;

    /// <summary>
    /// Returns a copy of the ticket byte array.
    /// </summary>
    public byte[] GetTicket() => _ticket != null ? (byte[])_ticket.Clone() : Array.Empty<byte>();

    /// <summary>
    /// Gets the ticket as a hexadecimal string.
    /// </summary>
    public string TicketString => _ticketString;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthTicket"/> struct.
    /// </summary>
    /// <param name="ticket">The authentication ticket byte array.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ticket"/> is null.</exception>
    public AuthTicket(byte[] ticket)
    {
    ArgumentNullException.ThrowIfNull(ticket);

        _ticket = (byte[])ticket.Clone();
        var authTicketString = new StringBuilder();
        foreach (var b in _ticket)
            authTicketString.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:x2}", b);
        _ticketString = authTicketString.ToString();
    }


    /// <summary>
    /// Determines whether the specified <see cref="AuthTicket"/> is equal to the current <see cref="AuthTicket"/>.
    /// </summary>
    public bool Equals(AuthTicket other) => _ticketString == other._ticketString;

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="AuthTicket"/>.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is AuthTicket other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => _ticketString != null ? _ticketString.GetHashCode(StringComparison.Ordinal) : 0;

    /// <summary>
    /// Returns a string representation of the ticket.
    /// </summary>
    public override string ToString() => _ticketString;


    /// <summary>
    /// Checks equality between two <see cref="AuthTicket"/> instances.
    /// </summary>
    public static bool operator ==(AuthTicket left, AuthTicket right)
    {
        return left.Equals(right);
    }


    /// <summary>
    /// Checks inequality between two <see cref="AuthTicket"/> instances.
    /// </summary>
    public static bool operator !=(AuthTicket left, AuthTicket right)
    {
        return !(left == right);
    }
}
