using System.Security.Claims;

namespace HouseKeeper.Modules.Households.Application;

public interface IIdentityEmailResolver
{
    Task<string?> ResolveVerifiedEmailAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
