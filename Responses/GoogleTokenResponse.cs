using System.Text.Json.Serialization;

namespace GymTracker.Responses
{
    public class GoogleTokenResponse
    {
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
    }
}