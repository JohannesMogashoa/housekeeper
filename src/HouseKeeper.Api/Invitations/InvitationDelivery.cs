using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

using HouseKeeper.Modules.Households.Application;

using Microsoft.Extensions.Options;

namespace HouseKeeper.Api.Invitations;

public sealed class InvitationDeliveryOptions
{
    public string FromAddress { get; set; } = string.Empty;
}

public sealed class DisabledInvitationDelivery : IInvitationDelivery
{
    public Task DeliverAsync(
        InvitationDeliveryMessage message,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class SesInvitationDelivery(
    IAmazonSimpleEmailServiceV2 client,
    IOptions<InvitationDeliveryOptions> options)
    : IInvitationDelivery
{
    public async Task DeliverAsync(
        InvitationDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        string fromAddress = options.Value.FromAddress.Trim();
        if (fromAddress.Length == 0)
        {
            throw new InvalidOperationException(
                "InvitationDelivery:FromAddress is required when SES delivery is enabled.");
        }

        await client.SendEmailAsync(
            new SendEmailRequest
            {
                FromEmailAddress = fromAddress,
                Destination = new Destination
                {
                    ToAddresses = [message.RecipientEmail]
                },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content
                        {
                            Data = $"You have been invited to {message.HouseholdName}"
                        },
                        Body = new Body
                        {
                            Text = new Content
                            {
                                Data = $"You have been invited to join {message.HouseholdName}. " +
                                    $"Open {message.InvitationUrl} before {message.ExpiresAtUtc:O} to accept."
                            }
                        }
                    }
                }
            },
            cancellationToken);
    }
}
