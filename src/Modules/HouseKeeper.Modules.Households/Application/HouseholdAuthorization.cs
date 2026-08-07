using HouseKeeper.Contracts.Households;
using HouseKeeper.Modules.Households.Domain;
using HouseKeeper.Modules.Households.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HouseKeeper.Modules.Households.Application;

public sealed class HouseholdAuthorization(HouseholdsDbContext dbContext)
    : HouseKeeper.Contracts.Households.IHouseholdAuthorization
{
    public async Task<HouseholdAccessDecision> AuthorizeAsync(
        Guid householdId,
        string subject,
        string requiredRole,
        CancellationToken cancellationToken = default)
    {
        HouseholdMember? member = await dbContext.Members
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.HouseholdId == householdId &&
                    candidate.Subject == subject &&
                    candidate.Status == MemberStatus.Active,
                cancellationToken);

        if (member is null)
        {
            return new HouseholdAccessDecision(false, null);
        }

        bool allowed = requiredRole == MemberRoleNames.Member ||
            member.Role.ToString() == requiredRole;
        return new HouseholdAccessDecision(allowed, member.MemberId);
    }
}
