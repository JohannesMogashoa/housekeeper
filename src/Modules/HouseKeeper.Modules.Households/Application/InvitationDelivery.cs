namespace HouseKeeper.Modules.Households.Application;

public interface IInvitationDelivery
{
    Task DeliverAsync(
        InvitationDeliveryMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record InvitationDeliveryMessage(
    string RecipientEmail,
    string HouseholdName,
    string InvitationUrl,
    DateTimeOffset ExpiresAtUtc);
