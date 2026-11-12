namespace PackingCalendars;

public sealed record DiningTable(string Id, int Seats, bool Accessible);
public sealed record Booking(string TableId, TimeRange Dining, int CleanupMinutes)
{
    public TimeRange Occupied
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(CleanupMinutes);
            return new TimeRange(Dining.Start, checked(Dining.End + CleanupMinutes));
        }
    }
}

public sealed record BookingRequest(int PartySize, int EarliestStart,
    int LatestStart, int DiningMinutes, bool NeedsAccessibleTable);
public sealed record Candidate(string TableId, int Start, int DiningEnd,
    int OccupiedEnd, int UnusedSeats, bool UsesUnneededAccessibleTable);

public static class Restaurant
{
    public static List<Candidate> FindCandidates(IEnumerable<DiningTable> tables,
        IEnumerable<Booking> bookings, TimeRange opening, BookingRequest request,
        int cleanupMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PartySize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.DiningMinutes);
        ArgumentOutOfRangeException.ThrowIfNegative(cleanupMinutes);
        if (request.LatestStart < request.EarliestStart)
            throw new ArgumentException("The arrival window is reversed.");

        var requiredMinutes = checked(request.DiningMinutes + cleanupMinutes);
        var occupiedByTable = bookings.ToLookup(booking => booking.TableId, booking => booking.Occupied);
        var candidates = new List<Candidate>();

        foreach (var table in tables)
        {
            // Hard constraints decide eligibility before preferences rank candidates.
            if (table.Seats < request.PartySize) continue;
            if (request.NeedsAccessibleTable && !table.Accessible) continue;

            var gaps = Availability.FindGaps(opening, occupiedByTable[table.Id], requiredMinutes);
            foreach (var gap in gaps)
            {
                var start = Math.Max(gap.Start, request.EarliestStart);
                if (start > request.LatestStart || requiredMinutes > gap.End - start) continue;

                candidates.Add(new Candidate(table.Id, start,
                    checked(start + request.DiningMinutes), checked(start + requiredMinutes),
                    table.Seats - request.PartySize,
                    table.Accessible && !request.NeedsAccessibleTable));
            }
        }

        // Policy: earliest start, then fewest unused seats, then preserve accessibility.
        return candidates.OrderBy(candidate => candidate.Start)
            .ThenBy(candidate => candidate.UnusedSeats)
            .ThenBy(candidate => candidate.UsesUnneededAccessibleTable)
            .ThenBy(candidate => candidate.TableId, StringComparer.Ordinal)
            .ToList();
    }
}
