using Mostlylucid.Packing.Calendar;

namespace PackingTutorial;

internal static class SalonDefinition
{
    internal static DateTimeOffset At(int hour) => new(2030, 6, 3, hour, 0, 0, TimeSpan.Zero);
    private static TimeSpan Minutes(int value) => TimeSpan.FromMinutes(value);
    private static HashSet<string> Set(params string[] values) => new(values, StringComparer.Ordinal);

    internal static CalendarDefinition Create()
    {
        var opening = new Period(At(9), At(17));
        return new CalendarDefinition("node-salon", opening, Minutes(5),
        [
            new("sarah", 1, Set("technician", "gels"), [opening]),
            new("anna", 1, Set("technician", "pedicure"), [opening]),
            new("manicure", 1, Set("manicure-station"), [opening]),
            new("pedicure", 1, Set("pedicure-station"), [opening]),
            new("lamp", 1, Set("drying-lamp"), [opening])
        ],
        [new("alice", true, Set()), new("beth", true, Set()), new("other", true, Set())],
        [
            new("gels", Minutes(45),
                [new("technician", Set("technician", "gels")),
                 new("station", Set("manicure-station")), new("lamp", Set("drying-lamp"))],
                [new("prepare", Minutes(0), Minutes(15), ["technician", "station"]),
                 new("dry", Minutes(15), Minutes(20), ["station", "lamp"]),
                 new("finish", Minutes(35), Minutes(10), ["technician", "station"]),
                 new("reset", Minutes(45), Minutes(10), ["station"])]),
            new("pedicure", Minutes(30),
                [new("technician", Set("technician", "pedicure")),
                 new("station", Set("pedicure-station"))],
                [new("service", Minutes(0), Minutes(30), ["technician", "station"]),
                 new("reset", Minutes(30), Minutes(5), ["station"])]),
            new("consult", Minutes(15), [new("technician", Set("technician"))],
                [new("service", Minutes(0), Minutes(15), ["technician"])])
        ]);
    }
}
