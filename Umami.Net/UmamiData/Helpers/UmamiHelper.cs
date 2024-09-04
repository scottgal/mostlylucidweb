namespace Umami.Net.UmamiData.Helpers;

public static class UmamiHelper
{
    public static long ToMilliseconds(this DateTime dateTime)
    {
        // Convert the DateTime to UTC to ensure consistency
        var dateTimeOffset = new DateTimeOffset(dateTime.ToUniversalTime());

        // Calculate the milliseconds since Unix epoch
        var milliseconds = dateTimeOffset.ToUnixTimeMilliseconds();

        return milliseconds;
    }
}