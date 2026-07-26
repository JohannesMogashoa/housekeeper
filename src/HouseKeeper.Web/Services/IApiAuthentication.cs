namespace HouseKeeper.Web.Services;

public interface IApiAuthentication
{
    Task<bool> IsAuthenticatedAsync();

    Task AttachAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    Task SignOutAsync();
}
