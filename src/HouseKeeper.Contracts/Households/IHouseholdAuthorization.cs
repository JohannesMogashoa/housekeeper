namespace HouseKeeper.Contracts.Households;

public interface IHouseholdAuthorization
{
    Task<HouseholdAccessDecision> AuthorizeAsync(
        Guid householdId,
        string subject,
        string requiredRole,
        CancellationToken cancellationToken = default);
}
