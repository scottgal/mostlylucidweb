using NailSalonScheduling;

if (args.Contains("--check"))
{
    SchedulingChecks.Run();
    return;
}

var technicians = DemoSalon.Technicians();
var roster = RosterPlanner.Build(technicians, DemoSalon.Needs());
Console.WriteLine("Staff roster:");
foreach (var shift in roster.Assigned.OrderBy(shift => shift.Time.Start).ThenBy(shift => shift.TechnicianId))
    Console.WriteLine($"  {shift.TechnicianId}: {DemoSalon.Clock(shift.Time.Start)}–{DemoSalon.Clock(shift.Time.End)} ({shift.NeedId})");
if (roster.Unfilled.Count > 0) throw new Exception("Review the unfilled work blocks before publishing.");

var planner = new AppointmentPlanner(technicians, DemoSalon.Stations(), DemoSalon.Clients(),
    DemoSalon.Services(), roster, DemoSalon.Opening,
    new[] { new Appointment("returning", "gel", "anna", "bench", 60) });
var request = new AppointmentRequest("guest", "gel", EarliestStart: 60, LatestStart: 120);
Console.WriteLine($"Gel manicure, 10:00–11:00 arrival: {planner.FindOffers(request).Reason}");

request = request with { LatestStart = 150 }; // Extend arrival to 11:30.
var offer = planner.FindOffers(request).Offers.First();
Console.WriteLine($"Offer: {offer.TechnicianId}, {offer.StationId}, {DemoSalon.Clock(offer.Start)}–{DemoSalon.Clock(offer.ServiceEnd)}");
var flexible = new AppointmentRequest("other", "gel", 60, 300, PreferredTechnicianId: "cara");
var preferences = new SlotPreferences(TargetStart: 240, PreviousTechnicianId: "cara");
var earliest = planner.FindOffers(flexible, limit: 1).Offers.First();
Console.WriteLine($"Earliest feasible: {earliest.TechnicianId} at {DemoSalon.Clock(earliest.Start)}");
var recommended = SlotRecommender.Recommend(planner, flexible, preferences, DemoSalon.Stations()).First();
Console.WriteLine($"Recommended: {recommended.Offer.TechnicianId} at {DemoSalon.Clock(recommended.Offer.Start)}, score {recommended.Score:F2}");

Console.WriteLine($"Booked chosen offer: {planner.TryBook(request, offer)}");
Console.WriteLine($"Blocked client: {planner.FindOffers(request with { ClientId = "blocked" }).Reason}");
Console.WriteLine($"Client excluding Anna: {planner.FindOffers(request with { ClientId = "excluded" }).Reason}");

var history = new[]
{
    new PastVisit("other", new DateOnly(2026, 10, 20), "gel", "cara", 240),
    new PastVisit("other", new DateOnly(2026, 10, 27), "gel", "cara", 245),
    new PastVisit("other", new DateOnly(2026, 11, 3), "gel", "cara", 240)
};
var learned = CustomerLearning.Learn("other", new DateOnly(2026, 11, 10), history);
if (learned is not null)
{
    var proposed = new AppointmentRequest("other", learned.ServiceId, 0, 420,
        PreferredTechnicianId: learned.TechnicianId);
    var predicted = SlotRecommender.Recommend(planner, proposed,
        new SlotPreferences(learned.TargetStart), DemoSalon.Stations()).FirstOrDefault();
    if (predicted is not null)
        Console.WriteLine($"Before asking: perhaps {learned.ServiceId} with {predicted.Offer.TechnicianId} at {DemoSalon.Clock(predicted.Offer.Start)} ({learned.SupportingVisits} past visits).");
}
