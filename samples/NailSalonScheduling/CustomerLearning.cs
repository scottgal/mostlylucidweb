namespace NailSalonScheduling;

public sealed record PastVisit(string ClientId, DateOnly Date, string ServiceId, string TechnicianId, int Start);
public sealed record LearnedBookingProfile(string ServiceId, string TechnicianId, int TargetStart,
    int SupportingVisits, bool UsedWeekdayContext);

public static class CustomerLearning
{
    public static LearnedBookingProfile? Learn(string clientId, DateOnly serviceDate,
        IEnumerable<PastVisit> history, int minimumSupport = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumSupport);
        var visits = history.Where(visit => visit.ClientId == clientId && visit.Date < serviceDate).ToArray();
        if (visits.Any(visit => visit.Start < 0 || visit.Start >= 1440))
            throw new ArgumentException("Historical starts must use the sample's minute coordinate system.");
        var sameWeekday = visits.Where(visit => visit.Date.DayOfWeek == serviceDate.DayOfWeek).ToArray();
        var contextual = sameWeekday.Length >= minimumSupport;
        var evidence = contextual ? sameWeekday : visits;
        var service = evidence.GroupBy(visit => visit.ServiceId)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).FirstOrDefault();
        if (service is null || service.Count() < minimumSupport) return null;
        var serviceVisits = service.ToArray();
        var technician = serviceVisits.GroupBy(visit => visit.TechnicianId)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).First().Key;
        // Keep the most common hour band: averaging morning and evening can invent a midday preference.
        var mostCommonHour = serviceVisits.GroupBy(visit => visit.Start / 60)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key).First();
        var starts = mostCommonHour.Select(visit => visit.Start).Order().ToArray();
        var target = starts[(starts.Length - 1) / 2]; // Lower median within the most common band.
        return new LearnedBookingProfile(service.Key, technician, target, service.Count(), contextual);
    }
}
