using PackingCalendars;

namespace NailSalonScheduling;

public sealed class AppointmentPlanner
{
    private readonly IReadOnlyList<Technician> _technicians;
    private readonly IReadOnlyList<Station> _stations;
    private readonly Dictionary<string, Client> _clients;
    private readonly Dictionary<string, SalonService> _services;
    private readonly TimeRange _opening;
    private readonly CapacityCalendar _calendar;
    private readonly int _stepMinutes;

    public AppointmentPlanner(IReadOnlyList<Technician> technicians, IReadOnlyList<Station> stations,
        IEnumerable<Client> clients, IEnumerable<SalonService> services, Roster publishedRoster,
        TimeRange opening, IEnumerable<Appointment> existing, int stepMinutes = 5)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepMinutes);
        if (opening.Start < 0 || opening.End > 1440)
            throw new ArgumentOutOfRangeException(nameof(opening), "This sample models one day.");
        if (publishedRoster.Unfilled.Count > 0)
            throw new ArgumentException("Review uncovered shifts before publishing this sample roster.");
        _technicians = technicians;
        _stations = stations;
        _clients = clients.ToDictionary(client => client.Id, StringComparer.Ordinal);
        _services = services.ToDictionary(service => service.Id, StringComparer.Ordinal);
        _opening = opening;
        _stepMinutes = stepMinutes;
        foreach (var service in _services.Values)
            if (service.Minutes <= 0 || service.Minutes > 1440 || service.TechnicianResetMinutes < 0 ||
                service.TechnicianResetMinutes > 1440 || service.StationResetMinutes < 0 || service.StationResetMinutes > 1440)
                throw new ArgumentException("Service durations or reset times are invalid.");
        var resources = technicians.Select(technician => new CalendarResource($"tech:{technician.Id}", 1,
                publishedRoster.Assigned.Where(shift => shift.TechnicianId == technician.Id)
                    .Select(shift => shift.Time).ToArray()))
            .Concat(stations.Select(station => new CalendarResource($"station:{station.Id}", station.Seats, new[] { opening })))
            .Concat(_clients.Values.Select(client => new CalendarResource($"client:{client.Id}", 1, new[] { opening })));
        _calendar = new CapacityCalendar(resources);
        var seed = existing.SelectMany(appointment => Demands(appointment.ClientId,
            _services[appointment.ServiceId], appointment.TechnicianId, appointment.StationId, appointment.Start)).ToArray();
        if (!_calendar.TryReserve(seed)) throw new ArgumentException("Existing appointments conflict with the roster or capacity.");
    }

    public OfferResult FindOffers(AppointmentRequest request, int limit = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        if (request.EarliestStart < 0 || request.LatestStart < request.EarliestStart)
            throw new ArgumentException("The arrival window is invalid.");
        if (!_clients.TryGetValue(request.ClientId, out var client)) return new("UnknownClient", Array.Empty<Offer>());
        if (!client.MayBook) return new("ClientNotAllowed", Array.Empty<Offer>());
        if (!_services.TryGetValue(request.ServiceId, out var service)) return new("UnknownService", Array.Empty<Offer>());
        var eligible = _technicians.Where(technician => technician.Services.Contains(service.Id) &&
            !client.ExcludedTechnicianIds.Contains(technician.Id)).ToArray();
        if (eligible.Length == 0) return new("NoEligibleTechnician", Array.Empty<Offer>());
        var stations = _stations.Where(station => station.Services.Contains(service.Id) &&
            (!request.NeedsAccessibleStation || station.Accessible)).ToArray();
        if (stations.Length == 0) return new("NoSuitableStation", Array.Empty<Offer>());

        var offers = new List<Offer>();
        var earliest = Math.Max(request.EarliestStart, _opening.Start);
        var remainder = (earliest - _opening.Start) % _stepMinutes;
        var first = remainder == 0 ? earliest : (long)earliest + _stepMinutes - remainder;
        var latest = Math.Min(request.LatestStart, _opening.End - service.Minutes);
        for (var start = first; start <= latest; start += _stepMinutes)
        foreach (var technician in eligible)
        foreach (var station in stations)
        {
            var demands = Demands(client.Id, service, technician.Id, station.Id, (int)start);
            if (_calendar.CanReserve(demands))
                offers.Add(new Offer(client.Id, service.Id, technician.Id, station.Id,
                    (int)start, (int)start + service.Minutes, demands));
        }
        var ordered = offers.OrderBy(offer => offer.Start)
            .ThenBy(offer => request.PreferredTechnicianId is not null && offer.TechnicianId != request.PreferredTechnicianId)
            .ThenBy(offer => offer.TechnicianId, StringComparer.Ordinal)
            .ThenBy(offer => offer.StationId, StringComparer.Ordinal).Take(limit).ToArray();
        return new(ordered.Length == 0 ? "NoAvailability" : null, ordered);
    }

    public bool TryBook(AppointmentRequest request, Offer chosen)
    {
        // Recreate the chosen offer from trusted policy and current capacity, not its supplied demands.
        var current = FindOffers(request, limit: int.MaxValue).Offers.FirstOrDefault(offer =>
            offer.ClientId == chosen.ClientId && offer.ServiceId == chosen.ServiceId &&
            offer.TechnicianId == chosen.TechnicianId && offer.StationId == chosen.StationId &&
            offer.Start == chosen.Start && offer.ServiceEnd == chosen.ServiceEnd);
        return current is not null && _calendar.TryReserve(current.Demands);
    }

    public static Demand[] Demands(string clientId, SalonService service, string technicianId, string stationId, int start)
    {
        var end = checked(start + service.Minutes);
        return new[]
        {
            new Demand($"tech:{technicianId}", new TimeRange(start, checked(end + service.TechnicianResetMinutes))),
            new Demand($"station:{stationId}", new TimeRange(start, checked(end + service.StationResetMinutes))),
            new Demand($"client:{clientId}", new TimeRange(start, end))
        };
    }
}
