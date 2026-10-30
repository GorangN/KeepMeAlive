using System.Security.Cryptography;
using System.Text;

namespace KeepMeAlive.Services;

/// <summary>Implements PKCE and callback validation without trusting browser-provided identity.</summary>
public static class OAuthProtocol
{
    /// <summary>Creates a cryptographically random URL-safe state or verifier.</summary>
    /// <returns>A 256-bit unguessable URL-safe string.</returns>
    public static string CreateRandomValue() => Base64Url(RandomNumberGenerator.GetBytes(32));
    /// <summary>Creates the RFC 7636 SHA-256 challenge for a verifier.</summary>
    /// <param name="verifier">The secret authorization-code verifier.</param>
    /// <returns>The base64url encoded SHA-256 hash.</returns>
    public static string CreateChallenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    /// <summary>Validates anti-forgery state and returns a successful authorization code.</summary>
    /// <param name="callback">The loopback callback URI.</param>
    /// <param name="state">The expected one-use state.</param>
    /// <returns>The authorization code.</returns>
    /// <exception cref="InvalidOperationException">The callback is invalid or authorization was denied.</exception>
    public static string ReadCode(Uri callback, string state)
    {
        var query = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in callback.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split('=', 2);
            if (parts.Length != 2 || !query.TryAdd(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1].Replace('+', ' '))))
            {
                throw new InvalidOperationException("The sign-in response is invalid. Please retry.");
            }
        }
        if (!query.TryGetValue("state", out var actual) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(actual), Encoding.UTF8.GetBytes(state)))
        {
            throw new InvalidOperationException("The sign-in response could not be verified. Please retry.");
        }
        if (query.ContainsKey("error") || !query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Sign-in was cancelled or denied.");
        }
        return code;
    }
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
