namespace HouseKeeper.Contracts.Households;

public sealed record CreateInvitationRequest(string Email);

public sealed record InvitationSummary(
    Guid Id,
    Guid HouseholdId,
    string State,
    DateTimeOffset InvitedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record InvitationPreview(
    string State,
    DateTimeOffset? ExpiresAtUtc,
    string? HouseholdName);

public sealed record AcceptInvitationResponse(
    Guid HouseholdId,
    Guid MemberId,
    string Role,
    bool AlreadyAccepted);

public sealed record HouseholdMemberSummary(
    Guid MemberId,
    Guid HouseholdId,
    string DisplayName,
    string Role,
    string Status,
    DateTimeOffset JoinedAtUtc,
    DateTimeOffset? RemovedAtUtc);
