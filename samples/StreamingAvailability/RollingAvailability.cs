namespace StreamingAvailability;

public sealed class RollingAvailability
{
    private readonly BookingLedger _source;
    private readonly object _gate = new();
    private readonly Dictionary<string, Cell[]> _rings = new(StringComparer.Ordinal);
    private readonly int _width;
    private long _start;
    private long _cursor;
    private long _writes;

    public RollingAvailability(BookingLedger source, TimeSpan horizon)
    {
        if (horizon <= TimeSpan.Zero || horizon.Ticks % Slots.Size.Ticks != 0 || horizon > TimeSpan.FromDays(90))
            throw new ArgumentException("Use a positive horizon of whole slots, at most 90 days.");
        _source = source;
        _width = checked((int)(horizon.Ticks / Slots.Size.Ticks));
        Rebuild();
    }

    public long Cursor { get { lock (_gate) return _cursor; } }
    public long CellsWritten { get { lock (_gate) return _writes; } }
    public long Start { get { lock (_gate) return _start; } }
    public long End { get { lock (_gate) return _start + _width; } }

    public void Rebuild()
    {
        lock (_gate)
        {
            _start = Slots.Floor(_source.Clock.GetUtcNow());
            var snapshot = _source.Capture(_start, _start + _width);
            _rings.Clear();
            Hydrate(snapshot, _start, _start + _width);
            _cursor = snapshot.Version;
        }
    }

    public void CatchUp()
    {
        lock (_gate)
            foreach (var change in _source.ReadAfter(_cursor)) ApplyCore(change);
    }

    public void Apply(CalendarEvent change) { lock (_gate) ApplyCore(change); }

    public Snapshot? TryCapture(long start, long end)
    {
        lock (_gate)
        {
            if (start < _start || end > _start + _width || end <= start) return null;
            var cells = new Dictionary<CellKey, Cell>();
            foreach (var resource in _rings.Keys)
            for (var slot = start; slot < end; slot++)
            {
                var cell = Read(resource, slot);
                if (cell != default) cells.Add(new CellKey(resource, slot), cell);
            }
            return new Snapshot(_cursor, _rings.Keys.ToArray(), cells.AsReadOnly(), Array.Empty<CalendarEvent>());
        }
    }

    public void Advance()
    {
        lock (_gate)
        {
            var next = Math.Max(_start, Slots.Floor(_source.Clock.GetUtcNow()));
            if (next == _start)
            {
                CatchUp();
                return;
            }
            if (next - _start >= _width)
            {
                Rebuild(); // No retained cells: loading the new window is cheaper than replaying old changes.
                return;
            }
            var oldEnd = _start + _width;
            var nextEnd = next + _width;
            var tailStart = Math.Max(oldEnd, next);
            // Both retained-cell events and entering-tail values are captured at the same source version.
            var snapshot = _source.Capture(tailStart, nextEnd, _cursor);
            foreach (var change in snapshot.Pending) ApplyCore(change);
            _start = next;
            Hydrate(snapshot, tailStart, nextEnd);
            if (_cursor != snapshot.Version) throw new InvalidOperationException("Snapshot and stream positions disagree.");
        }
    }

    public ViewAnswer Check(IReadOnlyList<Demand> demands)
    {
        lock (_gate)
        {
            if (demands.Count == 0) return new(Availability.Unavailable, _cursor);
            var additions = new Dictionary<CellKey, long>();
            foreach (var demand in demands)
            {
                if (demand.Start < _start || demand.End > _start + _width)
                    return new(Availability.OutsideWindow, _cursor);
                if (demand.End <= demand.Start || demand.Units <= 0 || Slots.At(demand.Start) < _source.Clock.GetUtcNow())
                    return new(Availability.Unavailable, _cursor);
                for (var slot = demand.Start; slot < demand.End; slot++)
                {
                    var key = new CellKey(demand.ResourceId, slot);
                    additions[key] = additions.GetValueOrDefault(key) + demand.Units;
                }
            }
            foreach (var pair in additions)
            {
                var cell = Read(pair.Key.ResourceId, pair.Key.Slot);
                if ((long)cell.Used + pair.Value > cell.Capacity) return new(Availability.Unavailable, _cursor);
            }
            return new(Availability.Available, _cursor);
        }
    }

    private void ApplyCore(CalendarEvent change)
    {
        if (change.Sequence <= _cursor) return; // Duplicate delivery or an event already covered by a snapshot.
        if (change.Sequence != _cursor + 1)
            throw new InvalidOperationException("Stream gap: fetch missing events or rebuild before advancing the cursor.");
        var updates = new Dictionary<CellKey, Cell>();
        foreach (var delta in change.Changes)
            for (var slot = Math.Max(delta.Start, _start); slot < Math.Min(delta.End, _start + _width); slot++)
            {
                var key = new CellKey(delta.ResourceId, slot);
                var old = updates.TryGetValue(key, out var staged) ? staged : Read(key.ResourceId, key.Slot);
                updates[key] = new Cell(delta.Capacity ?? old.Capacity, checked(old.Used + delta.UsedDelta));
            }
        if (updates.Values.Any(cell => cell.Capacity < 0 || cell.Used < 0 || cell.Used > cell.Capacity))
            throw new InvalidOperationException("Projection invariant failed; rebuild from the authoritative snapshot.");
        foreach (var pair in updates)
        {
            Ring(pair.Key.ResourceId)[Index(pair.Key.Slot)] = pair.Value;
            _writes++;
        }
        _cursor = change.Sequence;
    }

    private void Hydrate(Snapshot snapshot, long start, long end)
    {
        foreach (var resource in snapshot.Resources)
        {
            var ring = Ring(resource);
            for (var slot = start; slot < end; slot++)
            {
                ring[Index(slot)] = snapshot.Cells.GetValueOrDefault(new CellKey(resource, slot));
                _writes++;
            }
        }
    }

    private Cell Read(string resource, long slot) => _rings.TryGetValue(resource, out var ring) ? ring[Index(slot)] : default;
    private int Index(long slot) => checked((int)(slot % _width));
    private Cell[] Ring(string resource)
    {
        if (!_rings.TryGetValue(resource, out var ring)) _rings.Add(resource, ring = new Cell[_width]);
        return ring;
    }
}
