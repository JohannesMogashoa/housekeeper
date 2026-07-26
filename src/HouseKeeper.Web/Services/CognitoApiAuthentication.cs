using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace HouseKeeper.Web.Services;

public sealed class CognitoApiAuthentication(
    IAccessTokenProvider accessTokenProvider,
    AuthenticationStateProvider authenticationStateProvider)
    : IApiAuthentication
{
    public async Task<bool> IsAuthenticatedAsync()
    {
        AuthenticationState state = await authenticationStateProvider
            .GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true;
    }

    public async Task AttachAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        AccessTokenResult result = await accessTokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions());

        if (!result.TryGetToken(out AccessToken? token))
        {
            throw new InvalidOperationException(
                "The authentication session has expired. Sign in again to continue.");
        }

        request.Headers.Authorization = new("Bearer", token.Value);
    }

    public Task SignOutAsync() => Task.CompletedTask;
}
