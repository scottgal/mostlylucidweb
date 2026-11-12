using PackingCalendars;

namespace NailSalonScheduling;

internal static class SchedulingChecks
{
    private static int _count;
    public static void Run()
    {
        var range = new TimeRange(0, 30);
        var calendar = new CapacityCalendar(new[] { new CalendarResource("seat", 2, new[] { new TimeRange(0, 120) }) });
        Check(calendar.TryReserve(new[] { new Demand("seat", range) }), "first seat");
        Check(calendar.TryReserve(new[] { new Demand("seat", range) }), "second simultaneous seat");
        Check(!calendar.TryReserve(new[] { new Demand("seat", range) }), "third simultaneous seat rejected");
        Check(calendar.CanReserve(new[] { new Demand("seat", new TimeRange(30, 60), 2) }), "touching endpoints free both seats");
        Check(!calendar.CanReserve(new[] { new Demand("seat", new TimeRange(120, 121)) }), "outside opening window");
        Check(!calendar.CanReserve(new[] { new Demand("unknown", range) }), "unknown resource");
        Check(!calendar.CanReserve(new[] { new Demand("seat", range, 0) }), "zero demand");
        var bundle = new CapacityCalendar(new[] { new CalendarResource("seat", 1, new[] { new TimeRange(0, 120) }) });
        Check(!bundle.TryReserve(new[] { new Demand("seat", range), new Demand("seat", range) }), "combined demands tested together");
        Check(bundle.TryReserve(new[] { new Demand("seat", range) }), "failed bundle made no partial reservation");
        var split = new CapacityCalendar(new[] { new CalendarResource("tech", 1, new[] { new TimeRange(0, 60), new TimeRange(90, 120) }) });
        Check(!split.CanReserve(new[] { new Demand("tech", new TimeRange(30, 100)) }), "appointment cannot cross a break");
        var adjacent = new CapacityCalendar(new[] { new CalendarResource("tech", 1, new[] { new TimeRange(0, 60), new TimeRange(60, 120) }) });
        Check(adjacent.CanReserve(new[] { new Demand("tech", new TimeRange(30, 100)) }), "adjacent work windows form continuous coverage");

        var technicians = DemoSalon.Technicians();
        var roster = RosterPlanner.Build(technicians, DemoSalon.Needs());
        Check(roster.Unfilled.Count == 0, "all work blocks filled");
        Check(roster.Assigned.Single(shift => shift.NeedId == "morning-pedicure").TechnicianId == "bea", "scarce skill assigned first");
        Check(roster.Assigned.Single(shift => shift.NeedId == "morning-general").TechnicianId == "anna", "general work uses remaining eligible employee");
        Check(roster.Assigned.Single(shift => shift.NeedId == "afternoon-general").TechnicianId == "cara", "time off and daily limits respected");
        var impossible = RosterPlanner.Build(new[] { technicians[0] }, new[] { new ShiftNeed("pedicure", range, "pedicure") });
        Check(impossible.Unfilled.Count == 1 && impossible.Assigned.Count == 0, "uncovered skill remains visible");
        var overlapping = RosterPlanner.Build(new[] { technicians[0] }, new[] { new ShiftNeed("a", range), new ShiftNeed("b", range) });
        Check(overlapping.Assigned.Count == 1 && overlapping.Unfilled.Count == 1, "employee cannot work two simultaneous blocks");
        var limited = RosterPlanner.Build(new[] { technicians[0] with { MaxDailyMinutes = 30 } },
            new[] { new ShiftNeed("a", range), new ShiftNeed("b", new TimeRange(30, 60)) });
        Check(limited.Assigned.Count == 1, "daily-minute limit");

        AppointmentPlanner Planner(IEnumerable<Appointment>? existing = null) => new(technicians, DemoSalon.Stations(),
            DemoSalon.Clients(), DemoSalon.Services(), roster, DemoSalon.Opening, existing ?? Array.Empty<Appointment>());
        var planner = Planner(new[] { new Appointment("returning", "gel", "anna", "bench", 60) });
        var request = new AppointmentRequest("guest", "gel", 60, 120);
        Check(planner.FindOffers(request).Reason == "NoAvailability", "free seat is insufficient while the skilled employee is busy");
        request = request with { LatestStart = 150 };
        var offer = planner.FindOffers(request).Offers.First();
        Check(offer.TechnicianId == "anna" && offer.Start == 125 && offer.ServiceEnd == 185, "technician reset delays next arrival");
        Check(planner.FindOffers(request with { ClientId = "blocked" }).Reason == "ClientNotAllowed", "salon block precedes availability");
        Check(planner.FindOffers(request with { ClientId = "excluded", PreferredTechnicianId = "anna" }).Reason == "NoAvailability", "preferred employee cannot override exclusion");
        Check(planner.FindOffers(request with { ClientId = "missing" }).Reason == "UnknownClient", "unknown client");
        Check(planner.FindOffers(request with { ServiceId = "missing" }).Reason == "UnknownService", "unknown service");
        Check(planner.TryBook(request, offer), "chosen offer reserves the complete resource bundle");
        Check(!planner.TryBook(request, offer), "stale offer rejected without substituting another time");
        Check(planner.FindOffers(new AppointmentRequest("guest", "pedicure", 125, 125)).Reason == "NoAvailability", "client cannot see two employees at once");
        Check(Planner().FindOffers(new AppointmentRequest("guest", "gel", 0, 0)).Offers.Count > 0, "rostered employee is available for appointments");
        Check(Planner().FindOffers(new AppointmentRequest("guest", "gel", 230, 230)).Reason == "NoAvailability", "service and employee cleanup must fit roster");
        Check(Planner().FindOffers(new AppointmentRequest("guest", "gel", 420, 420)).Reason == "NoAvailability", "station cleanup must finish before closing");
        Check(Planner().FindOffers(new AppointmentRequest("guest", "gel", 241, 244)).Reason == "NoAvailability", "grid discretisation is explicit");

        var twoGel = new[] { technicians[0], technicians[2] with { TimeOff = Array.Empty<TimeRange>() } };
        var twoRoster = new Roster(new[] { new ShiftAssignment("a", "anna", new TimeRange(0, 240)), new ShiftAssignment("c", "cara", new TimeRange(0, 240)) }, Array.Empty<ShiftNeed>());
        var pooled = new AppointmentPlanner(twoGel, DemoSalon.Stations(), DemoSalon.Clients(), DemoSalon.Services(), twoRoster,
            DemoSalon.Opening, new[] { new Appointment("returning", "gel", "anna", "bench", 60) });
        Check(pooled.FindOffers(new AppointmentRequest("guest", "gel", 60, 60)).Offers.Single().TechnicianId == "cara", "one occupied bench seat leaves the other usable");
        var pooledOffer = pooled.FindOffers(new AppointmentRequest("guest", "gel", 60, 60)).Offers.Single();
        Check(pooled.TryBook(new AppointmentRequest("guest", "gel", 60, 60), pooledOffer), "second bench user books");
        Check(pooled.FindOffers(new AppointmentRequest("other", "gel", 60, 60)).Reason == "NoAvailability", "full bundle prevents third booking");
        var forged = Planner();
        var legitimate = forged.FindOffers(new AppointmentRequest("guest", "gel", 0, 0)).Offers.First();
        Check(forged.TryBook(new AppointmentRequest("guest", "gel", 0, 0), legitimate with { Demands = Array.Empty<Demand>() }), "confirmation rebuilds demands rather than trusting supplied allocations");
        Check(!forged.TryBook(new AppointmentRequest("other", "gel", 0, 0), legitimate), "offer belongs to its client");

        var flexible = new AppointmentRequest("guest", "gel", 60, 300, PreferredTechnicianId: "cara");
        var preferences = new SlotPreferences(240, PreviousTechnicianId: "cara");
        var ranked = SlotRecommender.Recommend(Planner(), flexible, preferences, DemoSalon.Stations());
        Check(ranked[0].Offer.TechnicianId == "cara" && ranked[0].Offer.Start == 240, "attributes can favour a later feasible slot");
        Check(Math.Abs(ranked[0].Score - 0.90) < 0.000001, "documented feature weights");
        Check(SlotRecommender.Recommend(Planner(), flexible with { ClientId = "blocked" }, preferences, DemoSalon.Stations()).Count == 0, "ranking cannot bypass salon block");
        Check(SlotRecommender.Recommend(Planner(), new AppointmentRequest("excluded", "gel", 0, 180, "anna"),
            new SlotPreferences(0, "anna"), DemoSalon.Stations()).Count == 0, "ranking cannot bypass technician exclusion");
        Check(ranked.All(slot => slot.Features.TimeFit is >= 0 and <= 1 && slot.Score is >= 0 and <= 1), "features and scores use documented ranges");
        var quietStations = DemoSalon.Stations().Append(new Station("quiet", "Quiet manicure seat", 1,
            DemoSalon.Set("gel"), true, Quiet: true)).ToArray();
        var quietPlanner = new AppointmentPlanner(technicians, quietStations, DemoSalon.Clients(), DemoSalon.Services(), roster,
            DemoSalon.Opening, Array.Empty<Appointment>());
        var quietChoice = SlotRecommender.Recommend(quietPlanner, new AppointmentRequest("guest", "gel", 0, 0),
            new SlotPreferences(0, PreferQuiet: true), quietStations).First();
        Check(quietChoice.Offer.StationId == "quiet", "station attributes influence eligible recommendations");
        var stillFeasible = Planner();
        Check(stillFeasible.TryBook(flexible, SlotRecommender.Recommend(stillFeasible, flexible, preferences, DemoSalon.Stations()).First().Offer),
            "recommended offer follows the same booking checks");

        var history = new[]
        {
            new PastVisit("guest", new DateOnly(2026, 10, 20), "gel", "cara", 240),
            new PastVisit("guest", new DateOnly(2026, 10, 27), "gel", "cara", 245),
            new PastVisit("guest", new DateOnly(2026, 11, 3), "gel", "cara", 240),
            new PastVisit("other", new DateOnly(2026, 11, 3), "pedicure", "bea", 60)
        };
        var learned = CustomerLearning.Learn("guest", new DateOnly(2026, 11, 10), history)!;
        Check(learned.ServiceId == "gel" && learned.TechnicianId == "cara" && learned.TargetStart == 240,
            "history produces a tentative service employee and time preference");
        Check(learned.SupportingVisits == 3 && learned.UsedWeekdayContext, "profile retains evidence support and context");
        Check(CustomerLearning.Learn("other", new DateOnly(2026, 11, 10), history) is null, "cold start has no invented personal profile");
        var fallback = CustomerLearning.Learn("guest", new DateOnly(2026, 11, 11), history)!;
        Check(!fallback.UsedWeekdayContext && fallback.SupportingVisits == 3, "weekday fallback is explicit");
        var predictedRequest = new AppointmentRequest("guest", learned.ServiceId, 0, 420, learned.TechnicianId);
        var predicted = SlotRecommender.Recommend(Planner(), predictedRequest, new SlotPreferences(learned.TargetStart), DemoSalon.Stations()).First();
        Check(predicted.Offer.TechnicianId == "cara" && predicted.Offer.Start == 240, "proactive intent still passes roster and capacity checks");
        var unrelated = history.Select(visit => visit with { ClientId = "guest" }).ToArray();
        Check(CustomerLearning.Learn("guest", new DateOnly(2026, 11, 10), unrelated)!.ServiceId == "gel", "most observed service wins over one alternative");

        var future = history.Concat(Enumerable.Range(0, 5).Select(i =>
            new PastVisit("guest", new DateOnly(2026, 11, 17), "pedicure", "bea", 0)));
        Check(CustomerLearning.Learn("guest", new DateOnly(2026, 11, 10), future)!.ServiceId == "gel", "future visits cannot leak into an earlier suggestion");

        // Independent minute-by-minute oracle for cumulative capacity and multi-demand bundles.
        var random = new Random(73);
        for (var trial = 0; trial < 500; trial++)
        {
            var capacity = random.Next(1, 5);
            var state = new CapacityCalendar(new[] { new CalendarResource("r", capacity, new[] { new TimeRange(0, 120) }) });
            var accepted = new List<Demand>();
            for (var attempt = 0; attempt < 15; attempt++)
            {
                var start = random.Next(0, 115);
                var demands = Enumerable.Range(0, random.Next(1, 3)).Select(_ => new Demand("r",
                    new TimeRange(start, Math.Min(120, start + random.Next(1, 25))), random.Next(1, capacity + 1))).ToArray();
                var expected = Enumerable.Range(0, 120).All(minute => accepted.Concat(demands)
                    .Where(demand => demand.Time.Start <= minute && minute < demand.Time.End).Sum(demand => demand.Units) <= capacity);
                Check(state.CanReserve(demands) == expected, "capacity oracle");
                Check(state.TryReserve(demands) == expected, "reserve agrees with capacity oracle");
                if (expected) accepted.AddRange(demands);
            }
        }
        Console.WriteLine($"Passed {_count} salon scheduling checks, including 500 generated capacity scenarios.");
    }

    private static void Check(bool condition, string name)
    {
        _count++;
        if (!condition) throw new InvalidOperationException("Failed: " + name);
    }
}
