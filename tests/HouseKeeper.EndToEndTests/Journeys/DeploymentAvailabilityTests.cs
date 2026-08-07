using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

using Xunit;

namespace HouseKeeper.EndToEndTests.Journeys;

public sealed class DeploymentAvailabilityTests : PageTest
{
    [Fact]
    public async Task PublishedPwaLoadsFromConfiguredDeploymentEndpoint()
    {
        string baseUrl = Environment.GetEnvironmentVariable("HOUSEKEEPER_WEB_BASE_URL")
            ?? "http://127.0.0.1:5136";

        IResponse? response = await Page.GotoAsync(baseUrl);

        Assert.NotNull(response);
        Assert.Equal(200, response.Status);
        await Expect(Page).ToHaveTitleAsync("HouseKeeper.Web");
        await Expect(Page.Locator("#app")).ToBeVisibleAsync();
    }
}
