using HouseKeeper.Modules.Households.Domain;
using HouseKeeper.Modules.Households.Persistence;

using Xunit;

namespace HouseKeeper.Modules.Households.Tests.Domain;

public sealed class HouseholdMemberTests
{
    [Fact]
    public void OwnerStartsActiveWithStableIdentity()
    {
        HouseholdMember owner = HouseholdMember.CreateOwner(
            Guid.NewGuid(),
            "owner-subject",
            DateTimeOffset.UtcNow);

        Assert.NotEqual(Guid.Empty, owner.MemberId);
        Assert.Equal(MemberRole.Owner, owner.Role);
        Assert.Equal(MemberStatus.Active, owner.Status);
    }

    [Fact]
    public void RemovalPreservesMemberIdentityAndCanBeReactivated()
    {
        DateTimeOffset joinedAt = DateTimeOffset.UtcNow.AddDays(-1);
        HouseholdMember member = HouseholdMember.CreateMember(
            Guid.NewGuid(),
            "member-subject",
            joinedAt);
        Guid memberId = member.MemberId;

        member.Remove(DateTimeOffset.UtcNow);
        Assert.Equal(MemberStatus.Removed, member.Status);
        Assert.Equal(memberId, member.MemberId);

        member.Reactivate(DateTimeOffset.UtcNow);
        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.Equal(memberId, member.MemberId);
        Assert.Null(member.RemovedAtUtc);
    }
}
