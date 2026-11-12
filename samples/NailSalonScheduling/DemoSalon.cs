using PackingCalendars;

namespace NailSalonScheduling;

public static class DemoSalon
{
    public static TimeRange Opening => new(0, 480); // One day, 09:00–17:00.
    public static Technician[] Technicians() => new[]
    {
        new Technician("anna", "Anna", Set("manicure", "gel"), new[] { Opening }, Array.Empty<TimeRange>(), 240),
        new Technician("bea", "Bea", Set("manicure", "pedicure"), new[] { Opening }, Array.Empty<TimeRange>(), 240),
        new Technician("cara", "Cara", Set("manicure", "gel", "pedicure"), new[] { Opening }, new[] { new TimeRange(0, 240) }, 240)
    };
    public static ShiftNeed[] Needs() => new[]
    {
        new ShiftNeed("morning-general", new TimeRange(0, 240)),
        new ShiftNeed("morning-pedicure", new TimeRange(0, 240), "pedicure"),
        new ShiftNeed("afternoon-general", new TimeRange(240, 480))
    };
    public static SalonService[] Services() => new[]
    {
        new SalonService("manicure", "Manicure", 30, 5, 10),
        new SalonService("gel", "Gel manicure", 60, 5, 10),
        new SalonService("pedicure", "Pedicure", 45, 5, 15)
    };
    public static Station[] Stations() => new[]
    {
        new Station("bench", "Manicure bench", 2, Set("manicure", "gel"), true),
        new Station("chair", "Pedicure chair", 1, Set("pedicure"), true)
    };
    public static Client[] Clients() => new[]
    {
        new Client("guest", true, Set()), new Client("returning", true, Set()),
        new Client("other", true, Set()), new Client("blocked", false, Set()),
        new Client("excluded", true, Set("anna"))
    };
    public static HashSet<string> Set(params string[] values) => new(values, StringComparer.Ordinal);
    public static string Clock(int minute) => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(9 * 60 + minute)).ToString("HH:mm");
}
