using Bunit;

using HouseKeeper.Contracts.Households;
using HouseKeeper.Web.Components;

using Xunit;

namespace HouseKeeper.Web.Tests.Components;

public sealed class HouseholdListTests
{
    [Fact]
    public void RenderShowsEmptyStateWhenNoHouseholdsExist()
    {
        using var context = new BunitContext();

        IRenderedComponent<HouseholdList> rendered = context.Render<HouseholdList>(parameters =>
            parameters.Add(component => component.Items, []));

        _ = rendered.Find("[data-testid='empty-households']");
        Assert.Empty(rendered.FindAll("article"));
    }

    [Fact]
    public void RenderShowsEveryHousehold()
    {
        using var context = new BunitContext();
        HouseholdSummary[] households =
        [
            new(
                Guid.NewGuid(),
                "Mogashoa Home",
                new DateTimeOffset(2026, 7, 18, 18, 0, 0, TimeSpan.Zero)),
            new(
                Guid.NewGuid(),
                "Weekend Cottage",
                new DateTimeOffset(2026, 7, 18, 19, 0, 0, TimeSpan.Zero))
        ];

        IRenderedComponent<HouseholdList> rendered = context.Render<HouseholdList>(parameters =>
            parameters.Add(component => component.Items, households));

        Assert.Equal(2, rendered.FindAll("article").Count);
        Assert.Contains("Mogashoa Home", rendered.Markup, StringComparison.Ordinal);
        Assert.Contains("Weekend Cottage", rendered.Markup, StringComparison.Ordinal);
    }
}
