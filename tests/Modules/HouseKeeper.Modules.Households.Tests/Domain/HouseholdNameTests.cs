using HouseKeeper.Modules.Households.Domain;

using Xunit;

namespace HouseKeeper.Modules.Households.Tests.Domain;

public sealed class HouseholdNameTests
{
    [Fact]
    public void TryCreateNormalizesValidName()
    {
        bool created = HouseholdName.TryCreate(
            "  Mogashoa Home  ",
            out HouseholdName? name,
            out string? error);

        Assert.True(created);
        Assert.NotNull(name);
        Assert.Equal("Mogashoa Home", name.Value);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void TryCreateRejectsMissingOrShortName(string? candidate)
    {
        bool created = HouseholdName.TryCreate(
            candidate,
            out HouseholdName? name,
            out string? error);

        Assert.False(created);
        Assert.Null(name);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreateRejectsNameOverMaximumLength()
    {
        string candidate = new('H', HouseholdName.MaxLength + 1);

        bool created = HouseholdName.TryCreate(
            candidate,
            out HouseholdName? name,
            out string? error);

        Assert.False(created);
        Assert.Null(name);
        Assert.NotNull(error);
    }
}
