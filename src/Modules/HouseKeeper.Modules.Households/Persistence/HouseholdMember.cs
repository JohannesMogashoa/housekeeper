namespace HouseKeeper.Modules.Households.Persistence;

internal sealed class HouseholdMember
{
    private HouseholdMember()
    {
    }

    private HouseholdMember(
        Guid householdId,
        string subject,
        string role,
        DateTimeOffset joinedAtUtc)
    {
        HouseholdId = householdId;
        Subject = subject;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    public Guid HouseholdId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public DateTimeOffset JoinedAtUtc { get; private set; }

    public static HouseholdMember CreateOwner(
        Guid householdId,
        string subject,
        DateTimeOffset joinedAtUtc) => new(householdId, subject, "Owner", joinedAtUtc);
}
