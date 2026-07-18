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
}
