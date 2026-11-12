using PackingCalendars;

namespace NailSalonScheduling;

public sealed record Technician(string Id, string Name, IReadOnlySet<string> Services,
    IReadOnlyList<TimeRange> Availability, IReadOnlyList<TimeRange> TimeOff, int MaxDailyMinutes);
public sealed record ShiftNeed(string Id, TimeRange Time, string? RequiredService = null);
public sealed record ShiftAssignment(string NeedId, string TechnicianId, TimeRange Time);
public sealed record Roster(IReadOnlyList<ShiftAssignment> Assigned, IReadOnlyList<ShiftNeed> Unfilled);

public sealed record SalonService(string Id, string Name, int Minutes,
    int TechnicianResetMinutes, int StationResetMinutes);
public sealed record Station(string Id, string Name, int Seats, IReadOnlySet<string> Services, bool Accessible, bool Quiet = false);
public sealed record Client(string Id, bool MayBook, IReadOnlySet<string> ExcludedTechnicianIds);
public sealed record AppointmentRequest(string ClientId, string ServiceId, int EarliestStart, int LatestStart,
    string? PreferredTechnicianId = null, bool NeedsAccessibleStation = false);
public sealed record Appointment(string ClientId, string ServiceId, string TechnicianId, string StationId, int Start);
public sealed record Offer(string ClientId, string ServiceId, string TechnicianId, string StationId,
    int Start, int ServiceEnd, IReadOnlyList<Demand> Demands);
public sealed record OfferResult(string? Reason, IReadOnlyList<Offer> Offers);
