namespace Mostlylucid.Shared.Helpers;

public static class DateFilterHelper
{
    // Date-picker end dates include that entire UTC calendar day.
    public static DateTimeOffset? InclusiveStart(DateTime? startDate) => startDate is null
        ? null
        : new DateTimeOffset(DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc));

    public static DateTimeOffset? ExclusiveEnd(DateTime? endDate) =>
        endDate is null || endDate.Value.Date == DateTime.MaxValue.Date
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc));
}
