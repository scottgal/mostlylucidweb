using PackingCalendars;

if (args.Contains("--check"))
{
    Checks.Run();
    return;
}

var sizes = new[] { 6, 8, 2, 4 };
PrintBins("First fit", BinPacking.FirstFit(sizes, 10));
PrintBins("Best fit", BinPacking.BestFit(sizes, 10));
PrintBins("First fit decreasing", BinPacking.FirstFit(sizes.OrderByDescending(size => size), 10));

// Minutes since 18:00, for one service ending at 22:00.
var opening = new TimeRange(0, 240);
var tables = new[]
{
    new DiningTable("A", 2, false),
    new DiningTable("B", 4, false),
    new DiningTable("C", 4, true),
    new DiningTable("D", 6, false)
};
var bookings = new[]
{
    new Booking("B", new TimeRange(0, 60), 15),     // 18:00–19:00, occupied until 19:15.
    new Booking("B", new TimeRange(150, 210), 15),  // 20:30–21:30, occupied until 21:45.
    new Booking("C", new TimeRange(30, 90), 15)     // 18:30–19:30, occupied until 19:45.
};
var request = new BookingRequest(
    PartySize: 3,
    EarliestStart: 60,    // 19:00
    LatestStart: 120,     // 20:00
    DiningMinutes: 60,
    NeedsAccessibleTable: true);
var candidates = Restaurant.FindCandidates(tables, bookings, opening, request, 15);
var choice = candidates.FirstOrDefault();
Console.WriteLine(choice is null
    ? "No suitable table in the arrival window."
    : $"Table {choice.TableId}: dine {Clock(choice.Start)}–{Clock(choice.DiningEnd)}, occupied until {Clock(choice.OccupiedEnd)}.");

static string Clock(int minute) => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(18 * 60 + minute)).ToString("HH:mm");
static void PrintBins(string name, List<List<int>> bins) =>
    Console.WriteLine($"{name}: {bins.Count} bins; {string.Join(" | ", bins.Select(bin => string.Join(" + ", bin)))}");
