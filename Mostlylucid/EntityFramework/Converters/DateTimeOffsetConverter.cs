using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mostlylucid.EntityFramework.Converters;

public class DateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public DateTimeOffsetConverter()
        : base(
            d => d.ToUniversalTime(),
            d => d.ToUniversalTime())
    {
    }
}