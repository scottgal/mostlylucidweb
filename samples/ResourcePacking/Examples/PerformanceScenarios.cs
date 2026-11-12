using System.Diagnostics;
using System.Globalization;

namespace ResourcePacking.Examples;

public static class PerformanceScenarios
{
    public static PackingProblem SalonAreas(int areas, bool sharedStation = false, int changedArea = -1,
        long version = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(areas);
        if (changedArea < -1 || changedArea >= areas) throw new ArgumentOutOfRangeException(nameof(changedArea));
        const int requestsPerArea = 8;
        var capacities = new Dictionary<DimensionId, long>();
        var requests = new List<Request>();
        var candidates = new List<Candidate>();
        for (var area = 0; area < areas; area++)
        {
            for (var client = 0; client < requestsPerArea; client++)
            {
                var request = string.Create(CultureInfo.InvariantCulture, $"area:{area}:client:{client}");
                requests.Add(new Request(request));
                for (var technician = 0; technician < 2; technician++)
                for (var option = 0; option < 4; option++)
                {
                    var start = option * 2;
                    var demands = new List<Consumption>();
                    for (var slot = start; slot < start + 4; slot++)
                    {
                        Add($"area:{area}:tech:{technician}:{slot}", area == changedArea && technician == 0 ? 0 : 1);
                        Add($"{request}:time:{slot}", 1);
                    }
                    for (var slot = start; slot < start + 5; slot++)
                        Add(sharedStation ? $"shared:station:{slot}" : $"area:{area}:station:{slot}",
                            sharedStation ? checked(areas * 2 - 1) : 2);
                    var preferredTime = client % 4;
                    var utility = 100 - 10 * Math.Abs(preferredTime - option) + (technician == client % 2 ? 5 : 0);
                    candidates.Add(new Candidate($"{request}:tech:{technician}:option:{option}", request, demands, [utility]));

                    void Add(string resource, int available)
                    {
                        var dimension = new DimensionId(resource);
                        capacities.TryAdd(dimension, available);
                        demands.Add(new Consumption(dimension, 1));
                    }
                }
            }
        }

        return new PackingProblem(capacities.Select(pair => new Capacity(pair.Key, pair.Value)), requests, candidates, [1],
            new PlanningSnapshot("measurement", 0, version, 0, 12));
    }

    public static void Run()
    {
        var budget = TimeSpan.FromSeconds(10);
        var native = new CpSatPackingSolver();
        native.Solve(SalonAreas(1), budget); // Load the native runtime before timing the comparisons.
        Console.WriteLine("Scenario | requests | candidates | rows | terms | build ms | full ms | selective ms | managed MiB full/selective | solved/reused | selected | utility");
        foreach (var (areas, shared) in new[] { (8, false), (32, false), (8, true) })
        {
            var incremental = new IncrementalPackingSolver(native);
            var initial = SalonAreas(areas, shared);
            if (incremental.Solve(initial, budget).Status != SolveStatus.Optimal)
                throw new InvalidOperationException("The initial cache warm-up did not prove an optimum.");
            var fullRuns = new List<TimeSpan>();
            var selectiveRuns = new List<TimeSpan>();
            var buildRuns = new List<TimeSpan>();
            var fullAllocations = new List<double>();
            var selectiveAllocations = new List<double>();
            IncrementalPackingResult? latest = null;
            PackingProblem? problem = null;
            for (var step = 0; step < 5; step++)
            {
                var timer = Stopwatch.StartNew();
                problem = SalonAreas(areas, shared, step % areas, step + 1);
                buildRuns.Add(timer.Elapsed);

                PackingResult expected;
                // Alternate the timing order to reduce an advantage from always running second.
                if (step % 2 == 0)
                {
                    expected = Full();
                    latest = Selective();
                }
                else
                {
                    latest = Selective();
                    expected = Full();
                }

                if (expected.Status != SolveStatus.Optimal || latest.Selection.Status != SolveStatus.Optimal ||
                    expected.Selected.Count != latest.Selection.Selected.Count || expected.Utility != latest.Selection.Utility)
                    throw new InvalidOperationException("Measurement plans did not reach the same proven objective.");

                PackingResult Full()
                {
                    timer.Restart();
                    var allocated = GC.GetAllocatedBytesForCurrentThread();
                    var result = native.Solve(problem, budget);
                    fullRuns.Add(timer.Elapsed);
                    fullAllocations.Add((GC.GetAllocatedBytesForCurrentThread() - allocated) / (1024.0 * 1024.0));
                    return result;
                }

                IncrementalPackingResult Selective()
                {
                    timer.Restart();
                    var allocated = GC.GetAllocatedBytesForCurrentThread();
                    var result = incremental.SolveWithStatistics(problem, budget);
                    selectiveRuns.Add(timer.Elapsed);
                    selectiveAllocations.Add((GC.GetAllocatedBytesForCurrentThread() - allocated) / (1024.0 * 1024.0));
                    return result;
                }
            }

            var terms = problem!.Candidates.Values.Sum(candidate => candidate.Demands.Count);
            var label = $"{areas} areas {(shared ? "shared" : "independent")}";
            var sizes = $"{problem.Requests.Count} | {problem.Candidates.Count} | {problem.Capacities.Count} | {terms}";
            var timings = FormattableString.Invariant(
                $"{Median(buildRuns):F2} | {Median(fullRuns):F2} | {Median(selectiveRuns):F2}");
            var allocations = FormattableString.Invariant(
                $"{fullAllocations.Order().ElementAt(2):F2}/{selectiveAllocations.Order().ElementAt(2):F2}");
            var work = latest!.Work;
            Console.WriteLine($"{label} | {sizes} | {timings} | {allocations} | " +
                              $"{work.SolvedComponents}/{work.ReusedComponents} | " +
                              $"{latest.Selection.Selected.Count} | {latest.Selection.Utility}");
        }
        Console.WriteLine("Times are medians of five updates in this process. Selective time includes partitioning, signatures and verification.");
        Console.WriteLine("The horizon is twelve five-minute slots. Managed allocations cover the calling thread and exclude native solver memory.");
    }

    private static double Median(IEnumerable<TimeSpan> times) => times.Select(time => time.TotalMilliseconds)
        .Order().ElementAt(2);
}
