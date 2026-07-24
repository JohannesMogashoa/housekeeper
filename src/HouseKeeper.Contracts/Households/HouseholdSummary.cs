namespace HouseKeeper.Contracts.Households;

public sealed record HouseholdSummary(Guid Id, string Name, DateTimeOffset CreatedAtUtc);
