namespace PackingCalendars;

public static class BinPacking
{
    public static List<List<int>> FirstFit(IEnumerable<int> sizes, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        var bins = new List<List<int>>();
        var remaining = new List<int>();

        foreach (var size in sizes)
        {
            if (size <= 0 || size > capacity)
                throw new ArgumentOutOfRangeException(nameof(sizes), "Items must be positive and fit in an empty bin.");

            var index = remaining.FindIndex(space => space >= size);
            if (index == -1)
            {
                bins.Add(new List<int>());
                remaining.Add(capacity);
                index = bins.Count - 1;
            }

            bins[index].Add(size);
            remaining[index] -= size;
        }

        return bins;
    }

    public static List<List<int>> BestFit(IEnumerable<int> sizes, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        var bins = new List<List<int>>();
        var remaining = new List<int>();
        foreach (var size in sizes)
        {
            if (size <= 0 || size > capacity)
                throw new ArgumentOutOfRangeException(nameof(sizes));
            var index = -1;
            for (var i = 0; i < remaining.Count; i++)
            {
                if (remaining[i] >= size && (index == -1 || remaining[i] < remaining[index]))
                {
                    index = i;
                }
            }
            if (index == -1)
            {
                bins.Add(new List<int>());
                remaining.Add(capacity);
                index = bins.Count - 1;
            }
            bins[index].Add(size);
            remaining[index] -= size;
        }
        return bins;
    }
}
