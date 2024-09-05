namespace Umami.Net.UmamiData.Models.RequestObjects;

public static class UnitEnumExtension
{
    public static string ToLowerString(this Unit unit)
    {
        return unit switch
        {
            Unit.year => "year",
            Unit.month => "month",
            Unit.hour => "hour",
            Unit.day => "day",
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }
}