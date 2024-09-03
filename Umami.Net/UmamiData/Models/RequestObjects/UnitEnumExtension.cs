namespace Umami.Net.UmamiData.Models.RequestObjects;

public static class UnitEnumExtension
{
    public static string ToLowerString(this Unit unit)
    {
        return unit switch
        {
            Unit.Year => "year",
            Unit.Month => "month",
            Unit.Hour => "hour",
            Unit.Day => "day",
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }
}