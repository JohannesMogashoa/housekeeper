using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;

using HouseKeeper.Modules.Households.Application;

namespace HouseKeeper.Api.Authentication;

public sealed class CognitoIdentityEmailResolver(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
    : IIdentityEmailResolver
{
    public async Task<string?> ResolveVerifiedEmailAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        string? subject = GetSubject(principal);
        string? claimEmail = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
        string? claimVerified = principal.FindFirst("email_verified")?.Value;
        if (subject is not null &&
            string.Equals(claimVerified, "true", StringComparison.OrdinalIgnoreCase) &&
            TryNormalizeEmail(claimEmail, out string? normalizedClaimEmail))
        {
            return normalizedClaimEmail;
        }

        string? authority = configuration["Authentication:Cognito:Authority"]?.TrimEnd('/');
        string? authorization = httpContextAccessor.HttpContext?
            .Request.Headers.Authorization
            .ToString();
        if (authority is null || authority.Length == 0 ||
            !AuthenticationHeaderValue.TryParse(authorization, out AuthenticationHeaderValue? bearer) ||
            !string.Equals(bearer.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(bearer.Parameter))
        {
            return null;
        }

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"{authority}/oauth2/userInfo");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            bearer.Parameter);

        using HttpResponseMessage response = await httpClientFactory
            .CreateClient("CognitoUserInfo")
            .SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        CognitoUserInfo? profile = await response.Content
            .ReadFromJsonAsync<CognitoUserInfo>(cancellationToken);
        if (profile is null ||
            subject is null ||
            !string.Equals(profile.Sub, subject, StringComparison.Ordinal) ||
            profile.EmailVerified != true ||
            !TryNormalizeEmail(profile.Email, out string? normalizedEmail))
        {
            return null;
        }

        return normalizedEmail;
    }

    private static string? GetSubject(ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? principal.FindFirst("sub")?.Value;

    private static bool TryNormalizeEmail(string? value, out string? normalized)
    {
        normalized = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
        return normalized is not null &&
            normalized.Contains('@', StringComparison.Ordinal) &&
            normalized.IndexOf('@') > 0 &&
            normalized.IndexOf('@') < normalized.Length - 1;
    }

    private sealed record CognitoUserInfo(
        [property: JsonPropertyName("sub")]
        string? Sub,
        [property: JsonPropertyName("email")]
        string? Email,
        [property: JsonPropertyName("email_verified")]
        bool? EmailVerified);
}
