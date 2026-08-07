namespace HouseKeeper.Contracts.Households;

public sealed record HouseholdAccessDecision(
    bool Allowed,
    Guid? MemberId);
