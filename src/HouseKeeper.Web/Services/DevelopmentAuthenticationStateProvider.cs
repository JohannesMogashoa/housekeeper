using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace HouseKeeper.Web.Services;

public sealed class DevelopmentAuthenticationStateProvider(DevelopmentSession session)
    : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        DevelopmentIdentity? identity = await session.LoadAsync();
        ClaimsIdentity claimsIdentity = identity is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, identity.Subject),
                    new Claim(ClaimTypes.Name, identity.DisplayName),
                    new Claim(ClaimTypes.Email, identity.Email),
                    new Claim("email_verified", "true")
                ],
                "Development");

        return new AuthenticationState(new ClaimsPrincipal(claimsIdentity));
    }
}
