using System;
using System.Net;
using Lidgren.Network;

namespace SFServer
{
    /// <summary>
    /// Extension methods for various types used in the server.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Truncates a string to the specified maximum length.
        /// </summary>
        /// <param name="value">The string to truncate.</param>
        /// <param name="maxLength">The maximum length of the string.</param>
        /// <returns>The truncated string.</returns>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Gets the sender IP address from a NetIncomingMessage.
        /// </summary>
        /// <param name="message">The incoming network message.</param>
        /// <returns>The IP address of the sender.</returns>
        public static IPAddress GetSenderIP(this NetIncomingMessage message)
        {
            if (message?.SenderEndPoint?.Address == null)
                return IPAddress.None;
            
            return message.SenderEndPoint.Address;
        }
    }
}
