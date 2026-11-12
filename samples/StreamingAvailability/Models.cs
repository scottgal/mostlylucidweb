namespace StreamingAvailability;

public static class Slots
{
    public static readonly TimeSpan Size = TimeSpan.FromMinutes(5);
    public static long Floor(DateTimeOffset time) => time.ToUnixTimeSeconds() / 300;
    public static long Exact(DateTimeOffset time)
    {
        if (time.ToUnixTimeSeconds() < 0 || time.UtcTicks % Size.Ticks != 0)
            throw new ArgumentException("Appointment boundaries must use the five-minute UTC grid.");
        return Floor(time);
    }
    public static DateTimeOffset At(long slot) => DateTimeOffset.FromUnixTimeSeconds(checked(slot * 300));
}

public sealed record Demand(string ResourceId, long Start, long End, int Units = 1)
{
    public static Demand Between(string resource, DateTimeOffset start, DateTimeOffset end, int units = 1) =>
        new(resource, Slots.Exact(start), Slots.Exact(end), units);
}

public readonly record struct CellKey(string ResourceId, long Slot);
public readonly record struct Cell(int Capacity, int Used);
public sealed record Change(string ResourceId, long Start, long End, int UsedDelta = 0, int? Capacity = null);
public sealed record CalendarEvent(long Sequence, string Kind, Guid? ReservationId, IReadOnlyList<Change> Changes);
public enum ReservationState { Held, Confirmed, Expired, Cancelled }
public sealed record Reservation(Guid Id, string Owner, IReadOnlyList<Demand> Demands,
    DateTimeOffset ExpiresAt, ReservationState State);
public sealed record Reply(bool Success, string Reason, Guid? ReservationId = null,
    DateTimeOffset? ExpiresAt = null, long Version = 0);
public sealed record Snapshot(long Version, IReadOnlyList<string> Resources,
    IReadOnlyDictionary<CellKey, Cell> Cells, IReadOnlyList<CalendarEvent> Pending);
public enum Availability { Available, Unavailable, OutsideWindow }
public sealed record ViewAnswer(Availability Availability, long Version);

public sealed class ManualClock(DateTimeOffset start) : TimeProvider
{
    private readonly object _gate = new();
    private DateTimeOffset _now = start;
    public override DateTimeOffset GetUtcNow() { lock (_gate) return _now; }
    public void Advance(TimeSpan by)
    {
        if (by < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(by));
        lock (_gate) _now += by;
    }
}
