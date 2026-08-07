using HouseKeeper.Modules.Households.Domain;

namespace HouseKeeper.Modules.Households.Persistence;

internal sealed class HouseholdMember
{
    private HouseholdMember()
    {
    }

    private HouseholdMember(
        Guid memberId,
        Guid householdId,
        string subject,
        MemberRole role,
        MemberStatus status,
        DateTimeOffset joinedAtUtc)
    {
        MemberId = memberId;
        HouseholdId = householdId;
        Subject = subject;
        Role = role;
        Status = status;
        JoinedAtUtc = joinedAtUtc;
    }

    public Guid MemberId { get; private set; }

    public Guid HouseholdId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public MemberRole Role { get; private set; } = MemberRole.Member;

    public MemberStatus Status { get; private set; } = MemberStatus.Removed;

    public DateTimeOffset JoinedAtUtc { get; private set; }

    public DateTimeOffset? RemovedAtUtc { get; private set; }

    public static HouseholdMember CreateOwner(
        Guid householdId,
        string subject,
        DateTimeOffset joinedAtUtc) => new(
            Guid.NewGuid(),
            householdId,
            subject,
            MemberRole.Owner,
            MemberStatus.Active,
            joinedAtUtc);

    public static HouseholdMember CreateMember(
        Guid householdId,
        string subject,
        DateTimeOffset joinedAtUtc) => new(
            Guid.NewGuid(),
            householdId,
            subject,
            MemberRole.Member,
            MemberStatus.Active,
            joinedAtUtc);

    public void Reactivate(DateTimeOffset joinedAtUtc)
    {
        Status = MemberStatus.Active;
        JoinedAtUtc = joinedAtUtc;
        RemovedAtUtc = null;
    }

    public void Remove(DateTimeOffset removedAtUtc)
    {
        Status = MemberStatus.Removed;
        RemovedAtUtc = removedAtUtc;
    }
}
