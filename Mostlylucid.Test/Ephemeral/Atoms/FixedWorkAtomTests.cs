using System.Collections.Concurrent;
using Mostlylucid.Helpers.Ephemeral.Atoms;
using Xunit;

namespace Mostlylucid.Test.Ephemeral.Atoms;

public class FixedWorkAtomTests
{
    [Fact]
    public async Task Executes_All_Items()
    {
        // Concurrent: the atom runs items on 2 workers, and List<T>.Add is not thread-safe -
        // with a plain List this test dropped items and failed intermittently.
        var processed = new ConcurrentBag<int>();
        await using var atom = new FixedWorkAtom<int>(
            async (item, ct) =>
            {
                await Task.Delay(5, ct);
                processed.Add(item);
            },
            maxConcurrency: 2,
            maxTracked: 10);

        var ids = new List<long>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(await atom.EnqueueAsync(i));
        }

        await atom.DrainAsync();
        Assert.Equal(5, processed.Count);
        Assert.Equal(5, atom.Stats().Completed);
        Assert.Equal(5, atom.Snapshot().Count);
        Assert.Equal(5, ids.Distinct().Count());
    }
}
