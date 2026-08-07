using HouseKeeper.Modules.Households.Domain;
using HouseKeeper.Modules.Households.Persistence;

using Xunit;

namespace HouseKeeper.Modules.Households.Tests.Domain;

public sealed class InvitationTests
{
    [Fact]
    public void TokenDigestDoesNotEqualRawTokenAndMatchesOnlyOriginal()
    {
        string token = InvitationToken.Create();
        string digest = InvitationToken.Digest(token);

        Assert.NotEqual(token, digest);
        Assert.True(InvitationToken.MatchesDigest(token, digest));
        Assert.False(InvitationToken.MatchesDigest(token + "x", digest));
    }

    [Fact]
    public void InvitationAcceptAndRevokeAreTerminalTransitions()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Invitation invitation = Invitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InvitationToken.Digest("invitee@example.test"),
            InvitationToken.Digest(InvitationToken.Create()),
            now,
            now.AddDays(7));

        Assert.Equal(InvitationState.Pending, invitation.State);
        invitation.Accept(Guid.NewGuid(), now.AddMinutes(1));
        Assert.Equal(InvitationState.Accepted, invitation.State);

        Assert.Throws<InvalidOperationException>(
            () => invitation.Revoke(now.AddMinutes(2)));
    }

    [Fact]
    public void ExpirationIsEvaluatedAgainstTheApprovedClockValue()
    {
        DateTimeOffset invitedAt = DateTimeOffset.UtcNow;
        Invitation invitation = Invitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InvitationToken.Digest("invitee@example.test"),
            InvitationToken.Digest(InvitationToken.Create()),
            invitedAt,
            invitedAt.AddDays(7));

        Assert.False(invitation.IsExpired(invitedAt.AddDays(7).AddTicks(-1)));
        Assert.True(invitation.IsExpired(invitedAt.AddDays(7)));
    }
}
