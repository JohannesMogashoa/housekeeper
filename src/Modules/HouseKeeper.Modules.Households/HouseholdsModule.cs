using System.Data;
using System.Diagnostics;
using System.Net.Mail;
using System.Security.Claims;

using HouseKeeper.Contracts.Households;
using HouseKeeper.Modules.Households.Application;
using HouseKeeper.Modules.Households.Diagnostics;
using HouseKeeper.Modules.Households.Domain;
using HouseKeeper.Modules.Households.Persistence;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HouseKeeper.Modules.Households;

public static class HouseholdsModule
{
    public static IServiceCollection AddHouseholdsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("HouseKeeper")
            ?? throw new InvalidOperationException(
                "Connection string 'HouseKeeper' is required.");

        services.AddDbContext<HouseholdsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    HouseholdsDbContext.Schema)));
        services.AddScoped<IHouseholdAuthorization, HouseholdAuthorization>();

        return services;
    }

    public static IEndpointRouteBuilder MapHouseholdsModule(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/households")
            .RequireAuthorization()
            .WithTags("Households");

        group.MapGet("/", ListHouseholdsAsync)
            .WithName("ListHouseholds");

        group.MapPost("/", CreateHouseholdAsync)
            .WithName("CreateHousehold");

        group.MapPost("/{householdId:guid}/invitations", CreateInvitationAsync)
            .WithName("CreateHouseholdInvitation");

        group.MapGet("/{householdId:guid}/invitations", ListInvitationsAsync)
            .WithName("ListHouseholdInvitations");

        group.MapPost(
                "/{householdId:guid}/invitations/{invitationId:guid}/revoke",
                RevokeInvitationAsync)
            .WithName("RevokeHouseholdInvitation");

        group.MapGet("/{householdId:guid}/members", ListMembersAsync)
            .WithName("ListHouseholdMembers");

        group.MapPost(
                "/{householdId:guid}/members/{memberId:guid}/remove",
                RemoveMemberAsync)
            .WithName("RemoveHouseholdMember");

        RouteGroupBuilder invitationGroup = endpoints
            .MapGroup("/api/invitations")
            .RequireAuthorization()
            .WithTags("Household invitations");

        invitationGroup.MapGet("/{token}/preview", PreviewInvitationAsync)
            .WithName("PreviewHouseholdInvitation");

        invitationGroup.MapPost("/{token}/accept", AcceptInvitationAsync)
            .WithName("AcceptHouseholdInvitation");

        return endpoints;
    }

    private static async Task<IResult> ListHouseholdsAsync(
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        List<HouseholdSummary> households = await (
            from household in dbContext.Households.AsNoTracking()
            join member in dbContext.Members.AsNoTracking()
                on household.Id equals member.HouseholdId
            where member.Subject == subject && member.Status == MemberStatus.Active
            orderby household.CreatedAtUtc
            select new HouseholdSummary(
                household.Id,
                household.Name,
                household.CreatedAtUtc,
                member.Role.ToString()))
            .ToListAsync(cancellationToken);

        return Results.Ok(households);
    }

    private static async Task<IResult> CreateHouseholdAsync(
        CreateHouseholdRequest request,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<HouseholdsDbContext> logger,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        if (!HouseholdName.TryCreate(request.Name, out HouseholdName? name, out string? error))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["name"] = [error ?? "Household name is invalid."]
                });
        }

        Guid householdId = Guid.NewGuid();
        DateTimeOffset now = timeProvider.GetUtcNow();
        Household household = Household.Create(householdId, name!, now);
        HouseholdMember owner = HouseholdMember.CreateOwner(householdId, subject, now);

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        dbContext.Add(household);
        dbContext.Add(owner);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        string traceId = Activity.Current?.TraceId.ToString() ?? "unavailable";
        HouseholdsLog.HouseholdCreated(logger, householdId, subject, traceId);

        HouseholdSummary response = new(
            household.Id,
            household.Name,
            household.CreatedAtUtc,
            MemberRoleNames.Owner);
        return Results.Created($"/api/households/{household.Id}", response);
    }

    private static async Task<IResult> CreateInvitationAsync(
        Guid householdId,
        CreateInvitationRequest request,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        IHouseholdAuthorization authorization,
        IInvitationDelivery delivery,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<HouseholdsDbContext> logger,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        HouseholdAccessDecision access = await authorization.AuthorizeAsync(
            householdId,
            subject,
            MemberRoleNames.Owner,
            cancellationToken);
        if (!access.Allowed || access.MemberId is null)
        {
            return Results.NotFound();
        }

        if (!TryNormalizeEmail(request.Email, out string? email))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["email"] = ["A valid email address is required."]
                });
        }

        Household? household = await dbContext.Households
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == householdId, cancellationToken);
        if (household is null)
        {
            return Results.NotFound();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string token = InvitationToken.Create();
        Invitation invitation = Invitation.Create(
            householdId,
            access.MemberId.Value,
            InvitationToken.Digest(email!),
            InvitationToken.Digest(token),
            now,
            now.AddDays(7));

        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        string webBaseUrl = configuration["InvitationDelivery:WebBaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException(
                "InvitationDelivery:WebBaseUrl is required.");
        string invitationUrl = $"{webBaseUrl}/invitations/{token}";

        try
        {
            await delivery.DeliverAsync(
                new InvitationDeliveryMessage(
                    email!,
                    household.Name,
                    invitationUrl,
                    invitation.ExpiresAtUtc),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            HouseholdsLog.InvitationDeliveryFailed(
                logger,
                invitation.Id,
                householdId,
                Activity.Current?.TraceId.ToString() ?? "unavailable");
            return Results.Problem(
                title: "Invitation delivery is temporarily unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "invitation_delivery_unavailable"
                });
        }

        string traceId = Activity.Current?.TraceId.ToString() ?? "unavailable";
        Guid inviterMemberId = access.MemberId.Value;
        HouseholdsLog.InvitationCreated(
            logger,
            invitation.Id,
            householdId,
            inviterMemberId,
            traceId);

        return Results.Created(
            $"/api/households/{householdId}/invitations/{invitation.Id}",
            ToSummary(invitation, now));
    }

    private static async Task<IResult> ListInvitationsAsync(
        Guid householdId,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        IHouseholdAuthorization authorization,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        HouseholdAccessDecision access = await authorization.AuthorizeAsync(
            householdId,
            subject,
            MemberRoleNames.Owner,
            cancellationToken);
        if (!access.Allowed)
        {
            return Results.NotFound();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        List<Invitation> invitations = await dbContext.Invitations
            .AsNoTracking()
            .Where(invitation => invitation.HouseholdId == householdId)
            .OrderByDescending(invitation => invitation.InvitedAtUtc)
            .ToListAsync(cancellationToken);

        return Results.Ok(invitations.Select(invitation => ToSummary(invitation, now)));
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid householdId,
        Guid invitationId,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        IHouseholdAuthorization authorization,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        HouseholdAccessDecision access = await authorization.AuthorizeAsync(
            householdId,
            subject,
            MemberRoleNames.Owner,
            cancellationToken);
        if (!access.Allowed)
        {
            return Results.NotFound();
        }

        Invitation? invitation = await dbContext.Invitations
            .SingleOrDefaultAsync(
                value => value.Id == invitationId && value.HouseholdId == householdId,
                cancellationToken);
        if (invitation is null)
        {
            return Results.NotFound();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (invitation.State != InvitationState.Pending || invitation.IsExpired(now))
        {
            return Results.Conflict(new { code = "invitation_not_pending" });
        }

        invitation.Revoke(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> PreviewInvitationAsync(
        string token,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (GetSubject(principal) is null)
        {
            return Results.Unauthorized();
        }

        Invitation? invitation = await dbContext.Invitations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.TokenDigest == InvitationToken.Digest(token),
                cancellationToken);
        if (invitation is null)
        {
            return Results.NotFound();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (invitation.State == InvitationState.Revoked)
        {
            return Results.Ok(new InvitationPreview("Revoked", null, null));
        }

        if (invitation.IsExpired(now))
        {
            return Results.Ok(new InvitationPreview("Expired", invitation.ExpiresAtUtc, null));
        }

        Household? household = await dbContext.Households
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == invitation.HouseholdId,
                cancellationToken);
        return Results.Ok(new InvitationPreview(
            invitation.State.ToString(),
            invitation.ExpiresAtUtc,
            household?.Name));
    }

    private static async Task<IResult> AcceptInvitationAsync(
        string token,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        IIdentityEmailResolver identityEmailResolver,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        string? email = await identityEmailResolver.ResolveVerifiedEmailAsync(
            principal,
            cancellationToken);
        if (subject is null || email is null)
        {
            return Results.Unauthorized();
        }

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        Invitation? invitation = await dbContext.Invitations
            .SingleOrDefaultAsync(
                value => value.TokenDigest == InvitationToken.Digest(token),
                cancellationToken);
        if (invitation is null)
        {
            return Results.NotFound();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (invitation.State == InvitationState.Revoked)
        {
            return Results.Conflict(new { code = "invitation_revoked" });
        }

        if (invitation.State == InvitationState.Accepted)
        {
            HouseholdMember? acceptedMember = invitation.AcceptedByMemberId is Guid memberId
                ? await dbContext.Members.SingleOrDefaultAsync(
                    member => member.MemberId == memberId,
                    cancellationToken)
                : null;
            if (acceptedMember?.Subject == subject)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new AcceptInvitationResponse(
                    invitation.HouseholdId,
                    acceptedMember.MemberId,
                    acceptedMember.Role.ToString(),
                    true));
            }

            return Results.Conflict(new { code = "invitation_already_used" });
        }

        if (invitation.IsExpired(now))
        {
            return Results.Conflict(new { code = "invitation_expired" });
        }

        if (!InvitationToken.MatchesDigest(email, invitation.TargetEmailDigest))
        {
            return Results.Forbid();
        }

        HouseholdMember? member = await dbContext.Members
            .SingleOrDefaultAsync(
                value => value.HouseholdId == invitation.HouseholdId &&
                    value.Subject == subject,
                cancellationToken);
        if (member is null)
        {
            member = HouseholdMember.CreateMember(invitation.HouseholdId, subject, now);
            dbContext.Members.Add(member);
        }
        else if (member.Status == MemberStatus.Removed)
        {
            member.Reactivate(now);
        }

        invitation.Accept(member.MemberId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(new AcceptInvitationResponse(
            invitation.HouseholdId,
            member.MemberId,
            member.Role.ToString(),
            false));
    }

    private static async Task<IResult> ListMembersAsync(
        Guid householdId,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        IHouseholdAuthorization authorization,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        HouseholdAccessDecision access = await authorization.AuthorizeAsync(
            householdId,
            subject,
            MemberRoleNames.Member,
            cancellationToken);
        if (!access.Allowed)
        {
            return Results.NotFound();
        }

        List<HouseholdMemberSummary> members = await dbContext.Members
            .AsNoTracking()
            .Where(member => member.HouseholdId == householdId)
            .OrderBy(member => member.JoinedAtUtc)
            .Select(member => new HouseholdMemberSummary(
                member.MemberId,
                member.HouseholdId,
                "Household member",
                member.Role.ToString(),
                member.Status.ToString(),
                member.JoinedAtUtc,
                member.RemovedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(members);
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid householdId,
        Guid memberId,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        IHouseholdAuthorization authorization,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        HouseholdAccessDecision access = await authorization.AuthorizeAsync(
            householdId,
            subject,
            MemberRoleNames.Owner,
            cancellationToken);
        if (!access.Allowed)
        {
            return Results.NotFound();
        }

        if (access.MemberId == memberId)
        {
            return Results.Conflict(new { code = "member_self_removal_not_allowed" });
        }

        HouseholdMember? member = await dbContext.Members
            .SingleOrDefaultAsync(
                value => value.MemberId == memberId && value.HouseholdId == householdId,
                cancellationToken);
        if (member is null)
        {
            return Results.NotFound();
        }

        if (member.Status == MemberStatus.Removed)
        {
            return Results.NoContent();
        }

        if (member.Role == MemberRole.Owner)
        {
            int ownerCount = await dbContext.Members.CountAsync(
                value => value.HouseholdId == householdId &&
                    value.Status == MemberStatus.Active &&
                    value.Role == MemberRole.Owner,
                cancellationToken);
            if (ownerCount <= 1)
            {
                return Results.Conflict(new { code = "last_owner_required" });
            }
        }

        member.Remove(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static InvitationSummary ToSummary(Invitation invitation, DateTimeOffset now) =>
        new(
            invitation.Id,
            invitation.HouseholdId,
            invitation.State == InvitationState.Pending && invitation.IsExpired(now)
                ? "Expired"
                : invitation.State.ToString(),
            invitation.InvitedAtUtc,
            invitation.ExpiresAtUtc);

    private static string? GetSubject(ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? principal.FindFirst("sub")?.Value;

    private static bool TryNormalizeEmail(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string candidate = value.Trim().ToLowerInvariant();
        try
        {
            MailAddress address = new(candidate);
            if (!string.Equals(address.Address, candidate, StringComparison.Ordinal))
            {
                return false;
            }

            normalized = candidate;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

}
