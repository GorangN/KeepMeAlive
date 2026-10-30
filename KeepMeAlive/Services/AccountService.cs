using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Retrieves profiles only through the authenticated Neon API.</summary>
public sealed class AccountService(IAuthenticationService authentication, HttpClient http, NeonOptions options) : IAccountService
{
    /// <inheritdoc/>
    public UserAccountProfile? Current { get; private set; }
    /// <inheritdoc/>
    public async Task<UserAccountProfile?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var token = await authentication.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token is null) { Clear(); return null; }
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(options.ApiBaseUrl), "v1/account"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadProfileAsync(response, cancellationToken).ConfigureAwait(false);
    }
    /// <inheritdoc/>
    public async Task<UserAccountProfile> UpdateProfileAsync(string displayName, CancellationToken cancellationToken = default)
    {
        displayName = displayName.Trim();
        if (displayName.Length is < 1 or > 100 || displayName.Any(char.IsControl))
        {
            throw new ArgumentException("Enter a name between 1 and 100 characters.", nameof(displayName));
        }
        var token = await authentication.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Sign in to update your profile.");
        using var request = new HttpRequestMessage(HttpMethod.Patch, new Uri(new Uri(options.ApiBaseUrl), "v1/profile"))
        {
            Content = JsonContent.Create(new { displayName }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadProfileAsync(response, cancellationToken).ConfigureAwait(false);
    }
    private async Task<UserAccountProfile> ReadProfileAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var profile = await response.Content.ReadFromJsonAsync<UserAccountProfile>(cancellationToken).ConfigureAwait(false) ?? throw new HttpRequestException("The account service returned an empty profile.");
        if (string.IsNullOrWhiteSpace(profile.UserId)) { throw new HttpRequestException("The account service returned an invalid profile."); }
        profile.LastSyncedAtUtc = DateTime.UtcNow;
        Current = profile;
        return profile;
    }
    /// <inheritdoc/>
    public void Clear() => Current = null;
}
