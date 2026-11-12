namespace PackingCalendars;

public sealed record TimeRange
{
    public int Start { get; }
    public int End { get; }
    public int Length => End - Start;

    public TimeRange(int start, int end)
    {
        if (end <= start) throw new ArgumentException("End must be after start.");
        Start = start;
        End = end;
    }

    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;
}

public static class Availability
{
    public static List<TimeRange> FindGaps(TimeRange opening,
        IEnumerable<TimeRange> occupied, int minimumLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumLength);
        var gaps = new List<TimeRange>();
        var cursor = opening.Start;

        foreach (var block in occupied.OrderBy(block => block.Start))
        {
            if (block.End <= opening.Start || block.Start >= opening.End) continue;
            var start = Math.Max(block.Start, opening.Start);
            var end = Math.Min(block.End, opening.End);

            if (start - cursor >= minimumLength)
                gaps.Add(new TimeRange(cursor, start));

            cursor = Math.Max(cursor, end);
        }

        if (opening.End - cursor >= minimumLength)
            gaps.Add(new TimeRange(cursor, opening.End));

        return gaps;
    }
}
