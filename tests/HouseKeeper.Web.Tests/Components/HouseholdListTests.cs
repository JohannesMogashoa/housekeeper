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
            new(Guid.NewGuid(), "Mogashoa Home", DateTimeOffset.Parse("2026-07-18T18:00:00Z")),
            new(Guid.NewGuid(), "Weekend Cottage", DateTimeOffset.Parse("2026-07-18T19:00:00Z"))
        ];

        IRenderedComponent<HouseholdList> rendered = context.Render<HouseholdList>(parameters =>
            parameters.Add(component => component.Items, households));

        Assert.Equal(2, rendered.FindAll("article").Count);
        Assert.Contains("Mogashoa Home", rendered.Markup, StringComparison.Ordinal);
        Assert.Contains("Weekend Cottage", rendered.Markup, StringComparison.Ordinal);
    }
}
