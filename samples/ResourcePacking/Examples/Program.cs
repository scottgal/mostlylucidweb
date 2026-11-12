using System.Diagnostics;
using NailSalonScheduling;
using PackingCalendars;
using ResourcePacking;
using ResourcePacking.Examples;

if (args.Contains("--benchmark"))
{
    PerformanceScenarios.Run();
    return;
}

var solver = new CpSatPackingSolver();
var incremental = new IncrementalPackingSolver(solver);
var budget = TimeSpan.FromSeconds(2);

var servers = solver.Solve(Scenarios.Servers(), budget);
Console.WriteLine($"Servers ({servers.Status}): {string.Join(", ", servers.Selected)}");

var container = new Box3D(4, 2, 2);
var boxes = BoxPacking.Generate(container, [new("first", new Box3D(2, 2, 2)), new("second", new Box3D(2, 2, 2))],
    [new(0, 0, 0), new(2, 0, 0)]);
var layout = solver.Solve(boxes.Problem, budget);
BoxPacking.Verify(container, boxes, layout);
Console.WriteLine($"3D placements ({layout.Status} over supplied anchors): {layout.Selected.Count} boxes.");

var origin = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
var shifts = solver.Solve(CalendarPacking.Shifts(origin, DemoSalon.Technicians(), DemoSalon.Needs()).Problem, budget);
Console.WriteLine($"Required shifts ({shifts.Status}): {shifts.Selected.Count}");

var (salon, _, clock, start, end, requests) = Scenarios.Salon(solver);
var timer = Stopwatch.StartNew();
var snapshot = salon.Capture(requests, start, end);
var buildTime = timer.Elapsed;
var greedy = new GreedyPackingSolver().Solve(snapshot.Problem, budget);
var optimal = salon.Plan(incremental, requests, start, end, budget);
Console.WriteLine($"Greedy booked {greedy.Selected.Count}; CP-SAT booked {optimal.Selection.Selected.Count}.");
foreach (var proposal in optimal.Proposals)
    Console.WriteLine($"  {proposal.Request.Appointment.ClientId}: {proposal.TechnicianId} at {DemoSalon.Clock(proposal.Start)}");

var anna = optimal.Proposals.Single(proposal => proposal.TechnicianId == "anna");
var rivalRequest = requests[1] with { Id = "rival", Appointment = requests[1].Appointment with { ClientId = "rival" } };
var rival = salon.Plan(solver, [rivalRequest], start, end, budget).Proposals.Single();
var hold = salon.TryHold(Guid.NewGuid(), anna.Request.Appointment.ClientId, anna, TimeSpan.FromMinutes(5));
Console.WriteLine($"Accepted proposal: {hold.Reason}");
Console.WriteLine($"Competing proposal: {salon.TryHold(Guid.NewGuid(), "rival", rival, TimeSpan.FromMinutes(5)).Reason}");

clock.Advance(TimeSpan.FromMinutes(5));
var refreshed = salon.Plan(incremental, [rivalRequest], start, end, budget);
Console.WriteLine($"After expiry: {refreshed.Proposals.Count} available proposal at version {refreshed.Selection.Snapshot!.AvailabilityVersion}.");

var serviceDay = DateOnly.FromDateTime(origin.UtcDateTime);
var visits = Enumerable.Range(1, 3).Select(week =>
    new PastVisit("flexible", serviceDay.AddDays(-7 * week), "manicure", "cara", 45));
var learned = CustomerLearning.Learn("flexible", serviceDay, visits);
if (learned is not null)
{
    var suggested = new SalonRequest("suggestion",
        new AppointmentRequest("flexible", learned.ServiceId, 0, 80, learned.TechnicianId),
        new SlotPreferences(learned.TargetStart));
    var best = salon.Plan(solver, [suggested], start, end, budget).Proposals.Single();
    Console.WriteLine($"Suggested from {learned.SupportingVisits} visits: {learned.ServiceId} with {best.TechnicianId} at {DemoSalon.Clock(best.Start)}.");
}

if (args.Contains("--measure"))
{
    Console.WriteLine($"Model: {snapshot.Problem.Requests.Count} requests, {snapshot.Problem.Candidates.Count} candidates, " +
                      $"{snapshot.Problem.Capacities.Count} capacity rows, " +
                      $"{snapshot.Problem.Candidates.Values.Sum(candidate => candidate.Demands.Count)} nonzero terms.");
    Console.WriteLine($"Snapshot and model input: {buildTime.TotalMilliseconds:F2} ms. " +
                      $"Greedy: {greedy.Elapsed.TotalMilliseconds:F2} ms. CP-SAT: {optimal.Selection.Elapsed.TotalMilliseconds:F2} ms.");
    var repeated = salon.Capture([rivalRequest], start, end);
    var reused = incremental.SolveWithStatistics(repeated.Problem, budget);
    Console.WriteLine($"Repeated plan: generated {repeated.GeneratedRequests} request templates; " +
                      $"solved {reused.Work.SolvedComponents}, reused {reused.Work.ReusedComponents} components.");
}
