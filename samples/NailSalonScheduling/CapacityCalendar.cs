using PackingCalendars;

namespace NailSalonScheduling;

public sealed record CalendarResource(string Id, int Capacity, IReadOnlyList<TimeRange> Windows);
public sealed record Demand(string ResourceId, TimeRange Time, int Units = 1);

// A single-threaded teaching calendar. Persistent multi-user writes need transaction coordination.
public sealed class CapacityCalendar
{
    private readonly Dictionary<string, CalendarResource> _resources;
    private readonly List<Demand> _reserved = new();

    public CapacityCalendar(IEnumerable<CalendarResource> resources)
    {
        _resources = resources.ToDictionary(resource => resource.Id, StringComparer.Ordinal);
        foreach (var resource in _resources.Values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resource.Id);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resource.Capacity);
        }
    }

    public bool CanReserve(IReadOnlyList<Demand> proposed)
    {
        foreach (var demand in proposed)
        {
            if (!_resources.TryGetValue(demand.ResourceId, out var resource) || demand.Units <= 0 ||
                !IsCovered(demand.Time, resource.Windows)) return false;
        }

        foreach (var group in proposed.GroupBy(demand => demand.ResourceId))
        {
            var resource = _resources[group.Key];
            var allocations = _reserved.Where(demand => demand.ResourceId == group.Key).Concat(group);
            var events = allocations.SelectMany(demand => new[]
            {
                (Time: demand.Time.Start, Delta: (long)demand.Units),
                (Time: demand.Time.End, Delta: -(long)demand.Units)
            });
            long used = 0;
            foreach (var instant in events.GroupBy(change => change.Time).OrderBy(instant => instant.Key))
            {
                // Apply arrivals and departures together: [start, end) permits back-to-back use.
                used += instant.Sum(change => change.Delta);
                if (used > resource.Capacity) return false;
            }
        }
        return true;
    }

    public bool TryReserve(IReadOnlyList<Demand> proposed)
    {
        if (!CanReserve(proposed)) return false;
        _reserved.AddRange(proposed);
        return true;
    }

    private static bool IsCovered(TimeRange time, IEnumerable<TimeRange> windows)
    {
        var cursor = time.Start;
        foreach (var window in windows.OrderBy(window => window.Start))
        {
            if (window.End <= cursor) continue;
            if (window.Start > cursor) return false;
            cursor = Math.Max(cursor, window.End);
            if (cursor >= time.End) return true;
        }
        return false;
    }
}
