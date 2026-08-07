namespace HouseKeeper.Web.Services;

public sealed class DevelopmentApiAuthentication(DevelopmentSession session)
    : IApiAuthentication
{
    public async Task<bool> IsAuthenticatedAsync() => await session.LoadAsync() is not null;

    public async Task AttachAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        DevelopmentIdentity identity = await session.LoadAsync()
            ?? throw new InvalidOperationException("Sign in before calling the API.");

        request.Headers.Add(DevelopmentSession.SubjectHeader, identity.Subject);
        request.Headers.Add(DevelopmentSession.DisplayNameHeader, identity.DisplayName);
        request.Headers.Add(DevelopmentSession.EmailHeader, identity.Email);
    }

    public Task SignOutAsync() => session.SignOutAsync().AsTask();
}
