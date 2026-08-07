using Microsoft.Extensions.Logging;

namespace HouseKeeper.Modules.Households.Diagnostics;

internal static partial class HouseholdsLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Household {HouseholdId} created by subject {Subject}. TraceId: {TraceId}")]
    public static partial void HouseholdCreated(
        ILogger logger,
        Guid householdId,
        string subject,
        string traceId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Invitation {InvitationId} created for household {HouseholdId} by member {MemberId}. TraceId: {TraceId}")]
    public static partial void InvitationCreated(
        ILogger logger,
        Guid invitationId,
        Guid householdId,
        Guid memberId,
        string traceId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Invitation delivery failed for invitation {InvitationId} in household {HouseholdId}. TraceId: {TraceId}")]
    public static partial void InvitationDeliveryFailed(
        ILogger logger,
        Guid invitationId,
        Guid householdId,
        string traceId);
}
