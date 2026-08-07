using System.Net;

using Bunit;

using HouseKeeper.Web.Pages;
using HouseKeeper.Web.Services;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace HouseKeeper.Web.Tests.Pages;

public sealed class InvitationTests
{
    [Fact]
    public void InvalidInvitationDoesNotRevealHouseholdDetails()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IApiAuthentication>(new AlwaysAuthenticated());
        context.Services.AddSingleton(new HouseKeeperApiClient(
            new HttpClient(new StubHandler(HttpStatusCode.NotFound))
            {
                BaseAddress = new Uri("http://localhost/")
            }));

        IRenderedComponent<Invitation> rendered = context.Render<Invitation>(parameters =>
            parameters.Add(component => component.Token, "invalid-token"));

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains(
                "This invitation is unavailable or no longer valid.",
                rendered.Markup,
                StringComparison.Ordinal);
            Assert.DoesNotContain("Secret Household", rendered.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class AlwaysAuthenticated : IApiAuthentication
    {
        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);

        public Task AttachAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SignOutAsync() => Task.CompletedTask;
    }

    private sealed class StubHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
