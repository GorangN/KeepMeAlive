using System.Security.Cryptography;
using System.Text.Json;

namespace KeepMeAlive.Services;

internal sealed class OAuthSession
{
    /// <summary>Identifies one interactive grant across refresh rotations and storage migration.</summary>
    public string SessionId { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    internal string GetSessionId() => !string.IsNullOrWhiteSpace(SessionId) ? SessionId :
        "legacy-" + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new[] { Issuer, ClientId, ApiBaseUrl, RefreshToken })));

    internal bool IsSameFamily(OAuthSession other) =>
        Issuer == other.Issuer && ClientId == other.ClientId && ApiBaseUrl == other.ApiBaseUrl &&
        GetSessionId() == other.GetSessionId();
}
