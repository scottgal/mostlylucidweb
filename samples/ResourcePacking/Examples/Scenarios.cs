using NailSalonScheduling;
using PackingCalendars;
using StreamingAvailability;

namespace ResourcePacking.Examples;

public static class Scenarios
{
    public static PackingProblem Servers()
    {
        var capacities = new[]
        {
            new Capacity(new DimensionId("small:cpu"), 4),
            new Capacity(new DimensionId("small:memory-gib"), 16),
            new Capacity(new DimensionId("large:cpu"), 8),
            new Capacity(new DimensionId("large:memory-gib"), 16)
        };
        var workloads = new[] { (Id: "api", Cpu: 4, Memory: 6), (Id: "jobs", Cpu: 6, Memory: 4),
            (Id: "cache", Cpu: 2, Memory: 10) };
        var placements = from workload in workloads
                         from server in new[] { "small", "large" }
                         select new Candidate($"{workload.Id}@{server}", workload.Id,
                             [new Consumption(new DimensionId($"{server}:cpu"), workload.Cpu),
                                 new Consumption(new DimensionId($"{server}:memory-gib"), workload.Memory)], []);
        return new PackingProblem(capacities, workloads.Select(workload => new Request(workload.Id)), placements);
    }

    public static (LiveSalon Salon, BookingLedger Ledger, ManualClock Clock, long Start, long End,
        SalonRequest[] Requests) Salon(IPackingSolver solver)
    {
        var day = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
        var opening = new TimeRange(0, 120);
        var start = Slots.Exact(day);
        var end = Slots.Exact(day.AddMinutes(opening.End));
        var clock = new ManualClock(day);
        var ledger = new BookingLedger(clock);
        var staff = new[]
        {
            new Technician("anna", "Anna", DemoSalon.Set("manicure"), [opening], [], 120),
            new Technician("cara", "Cara", DemoSalon.Set("manicure"), [opening], [], 120)
        };
        var stations = new[] { new Station("bench", "Manicure bench", 2, DemoSalon.Set("manicure"), true) };
        var clients = new[]
        {
            new Client("flexible", true, DemoSalon.Set()),
            new Client("anna-only", true, DemoSalon.Set("cara")),
            new Client("rival", true, DemoSalon.Set("cara")),
            new Client("blocked", false, DemoSalon.Set())
        };
        var needs = new[] { new ShiftNeed("first", opening), new ShiftNeed("second", opening) };
        var shiftChoices = CalendarPacking.Shifts(day, staff, needs);
        var shifts = solver.Solve(shiftChoices.Problem, TimeSpan.FromSeconds(2));
        if (shifts.Status is not (SolveStatus.Optimal or SolveStatus.Feasible))
            throw new InvalidOperationException($"Required shifts could not be filled: {shifts.Status}.");
        var roster = new Roster(shifts.Selected.Select(id => shiftChoices.Assignments[id]).ToArray(), []);
        foreach (var shift in roster.Assigned)
            Set($"tech:{shift.TechnicianId}", 1);
        foreach (var station in stations) Set($"station:{station.Id}", station.Seats);
        foreach (var client in clients) Set($"client:{client.Id}", 1);

        var requests = new[]
        {
            new SalonRequest("01-flexible", new AppointmentRequest("flexible", "manicure", 30, 30, "anna"),
                new SlotPreferences(30)),
            new SalonRequest("02-anna-only", new AppointmentRequest("anna-only", "manicure", 30, 30),
                new SlotPreferences(30))
        };

        return (new LiveSalon("demo", day, ledger, staff, stations, clients, DemoSalon.Services(), roster, opening),
            ledger, clock, start, end, requests);

        void Set(string resource, int capacity)
        {
            var reply = ledger.SetCapacity(Guid.NewGuid(), resource, start, end, capacity);
            if (!reply.Success) throw new InvalidOperationException(reply.Reason);
        }
    }
}
