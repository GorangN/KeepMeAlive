using System.Text.Json.Serialization;

namespace KeepMeAlive.Services;

internal sealed class OAuthMetadata
{
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;
    [JsonPropertyName("revocation_endpoint")]
    public string? RevocationEndpoint { get; set; }
}
