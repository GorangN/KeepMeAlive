namespace KeepMeAlive.Models;

/// <summary>Contains public endpoints and the public OAuth client identity.</summary>
public sealed class NeonOptions
{
    /// <summary>Gets or sets the API root URL.</summary>
    public string ApiBaseUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact Neon branch issuer, including its auth path.</summary>
    public string AuthIssuer { get; set; } = string.Empty;
    /// <summary>Gets or sets the registered public desktop client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>Gets or sets the API resource audience registered with Neon Auth.</summary>
    public string Resource { get; set; } = "https://keepmealive.app/api";
    /// <summary>Gets whether all required public settings are configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && IsHttps(ApiBaseUrl) && IsHttps(AuthIssuer) && IsHttps(Resource);
    private static bool IsHttps(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment);
}
