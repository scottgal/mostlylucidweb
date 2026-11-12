using System.Globalization;
using System.Text.Json;
using NailSalonScheduling;
using StreamingAvailability;
using CalendarDemand = NailSalonScheduling.Demand;
using LedgerDemand = StreamingAvailability.Demand;

namespace ResourcePacking.Examples;

public sealed record SalonRequest(string Id, AppointmentRequest Appointment, SlotPreferences Preferences);
public sealed record SalonCandidates(PackingProblem Problem, IReadOnlyDictionary<string, Offer> Offers,
    int GeneratedRequests = 0);
public sealed record ShiftCandidates(PackingProblem Problem, IReadOnlyDictionary<string, ShiftAssignment> Assignments);

public static class CalendarPacking
{
    public static ShiftCandidates Shifts(DateTimeOffset origin, IReadOnlyList<Technician> technicians,
        IReadOnlyList<ShiftNeed> needs)
    {
        var capacities = new Dictionary<DimensionId, long>();
        var candidates = new List<Candidate>();
        var assignments = new Dictionary<string, ShiftAssignment>(StringComparer.Ordinal);
        foreach (var technician in technicians)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(technician.MaxDailyMinutes);
            var windows = technician.Availability.SelectMany(window =>
                PackingCalendars.Availability.FindGaps(window, technician.TimeOff, minimumLength: 1)).ToArray();
            var calendar = new CapacityCalendar([new CalendarResource(technician.Id, 1, windows)]);
            var daily = new DimensionId($"minutes:{technician.Id}");
            capacities.Add(daily, technician.MaxDailyMinutes);

            foreach (var need in needs)
            {
                if (need.RequiredService is { } service && !technician.Services.Contains(service)) continue;
                var demand = new CalendarDemand(technician.Id, need.Time);
                if (!calendar.CanReserve([demand])) continue;
                var consumption = Expand(ToLedgerDemand(origin, demand)).ToArray();
                foreach (var cell in consumption) capacities.TryAdd(cell.Dimension, 1);
                var id = $"shift:{candidates.Count}";
                assignments.Add(id, new ShiftAssignment(need.Id, technician.Id, need.Time));
                candidates.Add(new Candidate(id, need.Id,
                    [.. consumption, new Consumption(daily, need.Time.Length)], []));
            }
        }

        return new ShiftCandidates(new PackingProblem(capacities.Select(pair => new Capacity(pair.Key, pair.Value)),
            needs.Select(need => new Request(need.Id, Required: true)), candidates), assignments.AsReadOnly());
    }

    internal static SalonCandidates Appointments(DateTimeOffset origin,
        IReadOnlyDictionary<string, IReadOnlyList<Offer>> generated,
        IReadOnlyDictionary<string, Station> stations, IReadOnlyList<SalonRequest> requests, Snapshot available,
        PlanningSnapshot snapshot, DateTimeOffset now, int candidateLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(candidateLimit);
        var capacities = new Dictionary<DimensionId, long>();
        var candidates = new List<Candidate>();
        var offers = new Dictionary<string, Offer>(StringComparer.Ordinal);
        var complete = true;
        foreach (var request in requests)
        {
            if (request.Preferences.TargetStart < request.Appointment.EarliestStart ||
                request.Preferences.TargetStart > request.Appointment.LatestStart)
                throw new ArgumentException("Target start must be inside the requested arrival window.");
            foreach (var offer in generated[request.Id])
            {
                var demands = offer.Demands.Select(demand => ToLedgerDemand(origin, demand)).ToArray();
                if (demands.Any(demand => demand.Start < snapshot.Start || demand.End > snapshot.End ||
                                          Slots.At(demand.Start) < now))
                    continue;
                if (candidates.Count == candidateLimit)
                {
                    complete = false;
                    break;
                }

                foreach (var demand in demands)
                for (var slot = demand.Start; slot < demand.End; slot++)
                {
                    var cell = available.Cells.GetValueOrDefault(new CellKey(demand.ResourceId, slot));
                    capacities.TryAdd(CellDimension(demand.ResourceId, slot), cell.Capacity - cell.Used);
                }

                var features = SlotRecommender.Describe(offer, request.Appointment, request.Preferences,
                    stations[offer.StationId]);
                var id = JsonSerializer.Serialize(new[] { request.Id, offer.TechnicianId, offer.StationId,
                    offer.Start.ToString(CultureInfo.InvariantCulture) });
                candidates.Add(new Candidate(id, request.Id, demands.SelectMany(Expand).ToArray(),
                    [Scale(features.TimeFit), Scale(features.PreferredTechnician), Scale(features.Continuity),
                        Scale(features.QuietStation)]));
                offers.Add(id, offer);
            }

            if (!complete) break;
        }

        return new SalonCandidates(new PackingProblem(capacities.Select(pair => new Capacity(pair.Key, pair.Value)),
                requests.Select(request => new Request(request.Id)), candidates, SlotRecommender.PreferenceWeights, snapshot,
                complete),
            offers.AsReadOnly());
    }

    public static LedgerDemand ToLedgerDemand(DateTimeOffset origin, CalendarDemand demand) =>
        LedgerDemand.Between(demand.ResourceId, origin.AddMinutes(demand.Time.Start),
            origin.AddMinutes(demand.Time.End), demand.Units);

    public static long Scale(double value)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(value), "A preference feature must be between zero and one.");
        return (long)Math.Round(value * 1_000, MidpointRounding.AwayFromZero);
    }

    private static DimensionId CellDimension(string resource, long slot) =>
        new(string.Create(CultureInfo.InvariantCulture, $"slot:{slot}:{resource}"));

    private static IEnumerable<Consumption> Expand(LedgerDemand demand)
    {
        for (var slot = demand.Start; slot < demand.End; slot++)
            yield return new Consumption(CellDimension(demand.ResourceId, slot), demand.Units);
    }
}
