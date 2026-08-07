using HouseKeeper.Modules.Households.Domain;

namespace HouseKeeper.Modules.Households.Persistence;

internal sealed class Invitation
{
    private Invitation()
    {
    }

    private Invitation(
        Guid id,
        Guid householdId,
        Guid inviterMemberId,
        string targetEmailDigest,
        string tokenDigest,
        DateTimeOffset invitedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        HouseholdId = householdId;
        InviterMemberId = inviterMemberId;
        TargetEmailDigest = targetEmailDigest;
        TokenDigest = tokenDigest;
        InvitedAtUtc = invitedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        UpdatedAtUtc = invitedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public Guid InviterMemberId { get; private set; }

    public string TargetEmailDigest { get; private set; } = string.Empty;

    public string TokenDigest { get; private set; } = string.Empty;

    public InvitationState State { get; private set; } = InvitationState.Pending;

    public DateTimeOffset InvitedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid? AcceptedByMemberId { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static Invitation Create(
        Guid householdId,
        Guid inviterMemberId,
        string targetEmailDigest,
        string tokenDigest,
        DateTimeOffset invitedAtUtc,
        DateTimeOffset expiresAtUtc) => new(
            Guid.NewGuid(),
            householdId,
            inviterMemberId,
            targetEmailDigest,
            tokenDigest,
            invitedAtUtc,
            expiresAtUtc);

    public void Accept(Guid memberId, DateTimeOffset acceptedAtUtc)
    {
        if (State != InvitationState.Pending)
        {
            throw new InvalidOperationException("Only pending invitations can be accepted.");
        }

        State = InvitationState.Accepted;
        AcceptedByMemberId = memberId;
        AcceptedAtUtc = acceptedAtUtc;
        UpdatedAtUtc = acceptedAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (State != InvitationState.Pending)
        {
            throw new InvalidOperationException("Only pending invitations can be revoked.");
        }

        State = InvitationState.Revoked;
        RevokedAtUtc = revokedAtUtc;
        UpdatedAtUtc = revokedAtUtc;
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
}
