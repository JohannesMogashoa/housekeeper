using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

using Xunit;

namespace HouseKeeper.EndToEndTests.Journeys;

public sealed class HouseholdBootstrapTests : PageTest
{
    [Fact]
    public async Task UserCanAuthenticateCreateHouseholdAndReloadIt()
    {
        string baseUrl = Environment.GetEnvironmentVariable("HOUSEKEEPER_WEB_BASE_URL")
            ?? "http://127.0.0.1:5136";
        string displayName = "HK-14 Browser User";
        string householdName = $"Browser Household {Guid.NewGuid():N}";

        _ = await Page.GotoAsync(baseUrl);

        await Page.GetByTestId("display-name").FillAsync(displayName);
        await Page.GetByTestId("sign-in").ClickAsync();

        await Expect(Page.GetByTestId("current-user"))
            .ToContainTextAsync(displayName);

        await Page.GetByTestId("household-name").FillAsync(householdName);
        await Page.GetByTestId("create-household").ClickAsync();

        ILocator householdList = Page.GetByTestId("household-list");
        await Expect(householdList).ToContainTextAsync(householdName);

        await Page.ReloadAsync();

        await Expect(Page.GetByTestId("current-user"))
            .ToContainTextAsync(displayName);
        await Expect(Page.GetByTestId("household-list"))
            .ToContainTextAsync(householdName);
    }
}
