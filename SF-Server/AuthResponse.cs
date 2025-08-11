using System.Text.Json.Serialization;

namespace SFServer;

/// <summary>
/// Represents a Steam authentication response from the Steam Web API.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthResponse"/> class.
    /// </summary>
    /// <param name="response">The JSON response from Steam Web API.</param>
    [JsonConstructor]
    public AuthResponse(JsonResponse response) => Response = response;

    /// <summary>
    /// Gets the JSON response containing authentication details.
    /// </summary>
    public JsonResponse Response { get; }

    /// <summary>
    /// Returns a string representation of the authentication response.
    /// </summary>
    /// <returns>A formatted string containing authentication details.</returns>
    public override string ToString()
    {
        var @params = Response.Params;

        return $"\nResult: {@params.Result}\nSteamid: {@params.Steamid}\nOwnersteamid: {@params.Ownersteamid}" +
               $"\nVacbanned: {@params.Vacbanned}\nPublisherbanned: {@params.Publisherbanned}";
    }

    /// <summary>
    /// Contains the parameters of a Steam authentication response.
    /// </summary>
    public class Params
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Params"/> class.
        /// </summary>
        /// <param name="result">The authentication result.</param>
        /// <param name="steamid">The user's Steam ID.</param>
        /// <param name="ownersteamid">The owner's Steam ID.</param>
        /// <param name="vacbanned">Whether the user is VAC banned.</param>
        /// <param name="publisherbanned">Whether the user is publisher banned.</param>
        [JsonConstructor]
        public Params(string result, string steamid, string ownersteamid, bool vacbanned, bool publisherbanned)
        {
            Result = result;
            Steamid = steamid;
            Ownersteamid = ownersteamid;
            Vacbanned = vacbanned;
            Publisherbanned = publisherbanned;
        }
        
        /// <summary>
        /// Gets the authentication result.
        /// </summary>
        public string Result { get; }
        
        /// <summary>
        /// Gets the user's Steam ID.
        /// </summary>
        public string Steamid { get; }
        
        /// <summary>
        /// Gets the owner's Steam ID.
        /// </summary>
        public string Ownersteamid { get; }
        
        /// <summary>
        /// Gets a value indicating whether the user is VAC banned.
        /// </summary>
        public bool Vacbanned { get; }
        
        /// <summary>
        /// Gets a value indicating whether the user is publisher banned.
        /// </summary>
        public bool Publisherbanned { get; }
    }

    /// <summary>
    /// Represents the JSON response wrapper from Steam Web API.
    /// </summary>
    public class JsonResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonResponse"/> class.
        /// </summary>
        /// <param name="params">The authentication parameters.</param>
        [JsonConstructor]
        public JsonResponse(Params @params) => Params = @params;

        /// <summary>
        /// Gets the authentication parameters.
        /// </summary>
        public Params Params { get; }
    }
}
