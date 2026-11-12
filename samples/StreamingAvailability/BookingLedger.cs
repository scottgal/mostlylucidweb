using System.Text.Json;
using System.Threading.Channels;

namespace StreamingAvailability;

// All state and the journal are in-memory. The lock owns one venue, not a distributed transaction.
public sealed class BookingLedger(TimeProvider clock)
{
    private readonly object _gate = new();
    private readonly Dictionary<CellKey, Cell> _cells = new();
    private readonly HashSet<string> _resources = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, Reservation> _reservations = new();
    private readonly Dictionary<Guid, (string Fingerprint, Reply Reply)> _commands = new();
    private readonly List<CalendarEvent> _journal = new();
    private readonly PriorityQueue<Guid, DateTimeOffset> _expiry = new();
    private readonly Channel<bool> _signals = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true, FullMode = BoundedChannelFullMode.DropWrite
    });

    public TimeProvider Clock => clock;
    public ChannelReader<bool> Changes => _signals.Reader;
    public long Version { get { lock (_gate) return _journal.Count; } }

    public Reply SetCapacity(Guid commandId, string resource, long start, long end, int capacity)
    {
        Validate(new Demand(resource, start, end));
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        var fingerprint = JsonSerializer.Serialize(new { Kind = "Capacity", resource, start, end, capacity });
        lock (_gate)
        {
            ExpireDue();
            if (Previous(commandId, fingerprint) is { } replay) return replay;
            for (var slot = start; slot < end; slot++)
                if (Read(resource, slot).Used > capacity)
                    return Remember(commandId, fingerprint, new(false, "CapacityInUse", Version: _journal.Count));
            _resources.Add(resource);
            for (var slot = start; slot < end; slot++)
            {
                var key = new CellKey(resource, slot);
                _cells[key] = Read(resource, slot) with { Capacity = capacity };
            }
            Append("CapacityChanged", null, new[] { new Change(resource, start, end, Capacity: capacity) });
            return Remember(commandId, fingerprint, new(true, "CapacityChanged", Version: _journal.Count));
        }
    }

    public Reply TryHold(Guid commandId, string owner, IReadOnlyList<Demand> demands, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        if (demands.Count == 0 || lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromHours(1))
            throw new ArgumentException("A hold needs demands and a positive lifetime of at most one hour.");
        foreach (var demand in demands) Validate(demand);
        var ordered = demands.OrderBy(d => d.ResourceId, StringComparer.Ordinal).ThenBy(d => d.Start)
            .ThenBy(d => d.End).ThenBy(d => d.Units).ToArray();
        var fingerprint = JsonSerializer.Serialize(new { Kind = "Hold", owner, ordered, Ttl = lifetime.Ticks });
        lock (_gate)
        {
            ExpireDue();
            if (Previous(commandId, fingerprint) is { } replay) return replay;
            var now = clock.GetUtcNow();
            if (ordered.Any(demand => Slots.At(demand.Start) < now))
                return Remember(commandId, fingerprint, new(false, "StartInPast", Version: _journal.Count));
            var additions = Expand(ordered);
            if (additions.Any(pair => (long)Read(pair.Key.ResourceId, pair.Key.Slot).Used + pair.Value >
                                      Read(pair.Key.ResourceId, pair.Key.Slot).Capacity))
                return Remember(commandId, fingerprint, new(false, "NoCapacity", Version: _journal.Count));
            var deadline = now + lifetime;
            var reservation = new Reservation(commandId, owner, Array.AsReadOnly(ordered), deadline, ReservationState.Held);
            _reservations.Add(commandId, reservation);
            Adjust(ordered, 1);
            _expiry.Enqueue(commandId, deadline);
            Append("HoldCreated", commandId, ToChanges(ordered, 1));
            return Remember(commandId, fingerprint, new(true, "Held", commandId, deadline, _journal.Count));
        }
    }

    public Reply Confirm(Guid commandId, string owner, Guid holdId)
    {
        var fingerprint = JsonSerializer.Serialize(new { Kind = "Confirm", owner, holdId });
        lock (_gate)
        {
            ExpireDue(); // Equality with the deadline is expired, even if the timer has not fired.
            if (Previous(commandId, fingerprint) is { } replay) return replay;
            if (!_reservations.TryGetValue(holdId, out var hold) || hold.Owner != owner)
                return Remember(commandId, fingerprint, new(false, "NotFound", Version: _journal.Count));
            if (hold.State != ReservationState.Held)
                return Remember(commandId, fingerprint, new(false, "HoldNotActive", holdId, hold.ExpiresAt, _journal.Count));
            _reservations[holdId] = hold with { State = ReservationState.Confirmed };
            Append("HoldConfirmed", holdId, Array.Empty<Change>()); // Already occupied: do not count twice.
            return Remember(commandId, fingerprint, new(true, "Confirmed", holdId, Version: _journal.Count));
        }
    }

    public Reply Cancel(Guid commandId, string owner, Guid reservationId)
    {
        var fingerprint = JsonSerializer.Serialize(new { Kind = "Cancel", owner, reservationId });
        lock (_gate)
        {
            ExpireDue();
            if (Previous(commandId, fingerprint) is { } replay) return replay;
            if (!_reservations.TryGetValue(reservationId, out var reservation) || reservation.Owner != owner)
                return Remember(commandId, fingerprint, new(false, "NotFound", Version: _journal.Count));
            if (reservation.State is not (ReservationState.Held or ReservationState.Confirmed))
                return Remember(commandId, fingerprint, new(false, "AlreadyInactive", reservationId, Version: _journal.Count));
            _reservations[reservationId] = reservation with { State = ReservationState.Cancelled };
            Adjust(reservation.Demands, -1);
            Append("ReservationCancelled", reservationId, ToChanges(reservation.Demands, -1));
            return Remember(commandId, fingerprint, new(true, "Cancelled", reservationId, Version: _journal.Count));
        }
    }

    public void Tick() { lock (_gate) ExpireDue(); }

    public Reservation? GetReservation(Guid id)
    {
        lock (_gate)
        {
            ExpireDue();
            return _reservations.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<CalendarEvent> ReadAfter(long after)
    {
        lock (_gate)
        {
            ExpireDue();
            return Pending(after);
        }
    }

    // Snapshot cells and their stream position under the same lock.
    // Pending events let a rolling view update retained cells before loading a newly exposed tail.
    public Snapshot Capture(long start, long end, long? after = null)
    {
        lock (_gate)
        {
            ExpireDue();
            var cells = new Dictionary<CellKey, Cell>();
            foreach (var resource in _resources)
                for (var slot = start; slot < end; slot++)
                {
                    var cell = Read(resource, slot);
                    if (cell != default) cells.Add(new CellKey(resource, slot), cell);
                }
            return new Snapshot(_journal.Count, _resources.Order(StringComparer.Ordinal).ToArray(), cells,
                after is { } cursor ? Pending(cursor) : Array.Empty<CalendarEvent>());
        }
    }

    private void ExpireDue()
    {
        var now = clock.GetUtcNow();
        while (_expiry.TryPeek(out var id, out var deadline) && deadline <= now)
        {
            _expiry.Dequeue();
            var hold = _reservations[id];
            if (hold.State != ReservationState.Held || hold.ExpiresAt != deadline) continue;
            _reservations[id] = hold with { State = ReservationState.Expired };
            Adjust(hold.Demands, -1);
            Append("HoldExpired", id, ToChanges(hold.Demands, -1));
        }
    }

    private Cell Read(string resource, long slot) => _cells.GetValueOrDefault(new CellKey(resource, slot));

    private void Adjust(IEnumerable<Demand> demands, int sign)
    {
        foreach (var pair in Expand(demands))
        {
            var cell = Read(pair.Key.ResourceId, pair.Key.Slot);
            var used = checked(cell.Used + (int)(sign * pair.Value));
            if (used < 0 || used > cell.Capacity) throw new InvalidOperationException("Capacity accounting failed.");
            _cells[pair.Key] = cell with { Used = used };
        }
    }

    private static Dictionary<CellKey, long> Expand(IEnumerable<Demand> demands)
    {
        var result = new Dictionary<CellKey, long>();
        foreach (var demand in demands)
            for (var slot = demand.Start; slot < demand.End; slot++)
            {
                var key = new CellKey(demand.ResourceId, slot);
                result[key] = result.GetValueOrDefault(key) + demand.Units;
            }
        return result;
    }

    private static Change[] ToChanges(IEnumerable<Demand> demands, int sign) => demands
        .Select(demand => new Change(demand.ResourceId, demand.Start, demand.End, checked(sign * demand.Units))).ToArray();

    private void Append(string kind, Guid? id, IReadOnlyList<Change> changes)
    {
        _journal.Add(new CalendarEvent(_journal.Count + 1L, kind, id, Array.AsReadOnly(changes.ToArray())));
        _signals.Writer.TryWrite(true); // Wake-up only: the journal, not this bounded channel, holds the events.
    }

    private IReadOnlyList<CalendarEvent> Pending(long after)
    {
        if (after < 0 || after > _journal.Count) throw new ArgumentOutOfRangeException(nameof(after));
        return _journal.Skip(checked((int)after)).ToArray();
    }

    private Reply? Previous(Guid id, string fingerprint)
    {
        if (!_commands.TryGetValue(id, out var existing)) return null;
        if (existing.Fingerprint != fingerprint)
            throw new InvalidOperationException("An idempotency key was reused for a different command.");
        return existing.Reply;
    }

    private Reply Remember(Guid id, string fingerprint, Reply reply)
    {
        _commands.Add(id, (fingerprint, reply));
        return reply;
    }

    private static void Validate(Demand demand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(demand.ResourceId);
        if (demand.Start < 0 || demand.End <= demand.Start || demand.End - demand.Start > 31 * 288 || demand.Units <= 0)
            throw new ArgumentException("A demand needs positive units and an interval of at most 31 days.");
    }
}
