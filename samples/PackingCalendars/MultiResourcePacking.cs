namespace PackingCalendars;

// Independent resource limits, rather than the geometry of boxes.
public sealed record Resources(int CpuCores, int MemoryGb);

public static class MultiResourcePacking
{
    public static bool CanAdd(Resources used, Resources item, Resources capacity)
    {
        if (used.CpuCores < 0 || used.MemoryGb < 0 || item.CpuCores < 0 || item.MemoryGb < 0 ||
            capacity.CpuCores < 0 || capacity.MemoryGb < 0)
            throw new ArgumentOutOfRangeException(nameof(item), "Resource amounts cannot be negative.");

        return item.CpuCores <= capacity.CpuCores - used.CpuCores &&
               item.MemoryGb <= capacity.MemoryGb - used.MemoryGb;
    }
}
