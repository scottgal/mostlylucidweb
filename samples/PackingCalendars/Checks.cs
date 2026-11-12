namespace PackingCalendars;

internal static class Checks
{
    private static int _count;
    public static void Run()
    {
        var sizes = new[] { 6, 8, 2, 4 };
        Check(BinPacking.FirstFit(sizes, 10).Count == 3, "first-fit counterexample");
        Check(BinPacking.BestFit(sizes, 10).Count == 2, "best fit fills the smaller gap");
        Check(BinPacking.FirstFit(sizes.OrderByDescending(x => x), 10).Count == 2, "decreasing order");
        Check(BinPacking.FirstFit(Array.Empty<int>(), 10).Count == 0, "empty packing");
        Throws(() => BinPacking.FirstFit(new[] { 11 }, 10), "oversized item");
        Throws(() => BinPacking.FirstFit(new[] { 0 }, 10), "zero item");
        Throws(() => BinPacking.FirstFit(sizes, 0), "zero capacity");
        Check(!new TimeRange(0, 60).Overlaps(new TimeRange(60, 90)), "touching boundaries");
        Check(new TimeRange(0, 60).Overlaps(new TimeRange(59, 90)), "one-minute overlap");
        Throws(() => new TimeRange(20, 20), "zero duration");
        var opening = new TimeRange(0, 240);
        Check(Availability.FindGaps(opening, Array.Empty<TimeRange>(), 60).SequenceEqual(new[] { opening }), "empty calendar");
        var blocks = new[] { new TimeRange(150, 225), new TimeRange(0, 75) };
        Check(Availability.FindGaps(opening, blocks, 75).SequenceEqual(new[] { new TimeRange(75, 150) }), "exact gap and unsorted input");
        Check(Availability.FindGaps(opening, blocks, 76).Count == 0, "fragmentation");
        Check(Availability.FindGaps(opening, new[] { new TimeRange(0, 240), new TimeRange(20, 40) }, 1).Count == 0, "nested occupied ranges");
        Check(Availability.FindGaps(opening, new[] { new TimeRange(-20, 10), new TimeRange(230, 260) }, 1)
            .SequenceEqual(new[] { new TimeRange(10, 230) }), "clipping to opening hours");
        Throws(() => Availability.FindGaps(opening, blocks, 0), "invalid minimum duration");

        var tables = new[] { new DiningTable("A", 4, false), new DiningTable("B", 4, true) };
        var request = new BookingRequest(3, 60, 120, 60, true);
        var booking = new Booking("B", new TimeRange(30, 90), 15);
        var candidates = Restaurant.FindCandidates(tables, new[] { booking }, opening, request, 15);
        Check(candidates.Count == 1 && candidates[0].TableId == "B" && candidates[0].Start == 105, "accessibility and cleanup");
        Check(candidates[0].DiningEnd == 165 && candidates[0].OccupiedEnd == 180, "dining and occupancy differ");
        Check(Restaurant.FindCandidates(tables, new[] { booking }, opening, request with { LatestStart = 104 }, 15).Count == 0, "latest arrival");
        Check(Restaurant.FindCandidates(tables, Array.Empty<Booking>(), opening, request with { PartySize = 5 }, 15).Count == 0, "capacity hard constraint");
        Check(Restaurant.FindCandidates(tables, Array.Empty<Booking>(), opening,
            request with { EarliestStart = 180, LatestStart = 180 }, 15).Count == 0, "cleanup must fit before close");
        Check(Restaurant.FindCandidates(tables.Reverse(), Array.Empty<Booking>(), opening,
            request with { NeedsAccessibleTable = false }, 15)[0].TableId == "A", "preserve a scarce feature when other scores tie");
        Throws(() => Restaurant.FindCandidates(tables, Array.Empty<Booking>(), opening, request with { PartySize = 0 }, 15), "invalid party size");
        Throws(() => Restaurant.FindCandidates(tables, Array.Empty<Booking>(), opening, request, -1), "negative cleanup");

        var gapBookings = new[]
        {
            new Booking("A", new TimeRange(0, 60), 15),
            new Booking("A", new TimeRange(150, 210), 15)
        };
        var exact = new BookingRequest(3, 75, 75, 60, false);
        Check(Restaurant.FindCandidates(new[] { tables[0] }, gapBookings, opening, exact, 15).Count == 1, "occupied interval exactly fills gap");
        Check(Restaurant.FindCandidates(new[] { tables[0] }, gapBookings, opening,
            exact with { EarliestStart = 76, LatestStart = 76 }, 15).Count == 0, "arrival inside gap leaves insufficient time");
        var capacity = new Resources(8, 16);
        var used = new Resources(6, 4);
        Check(!MultiResourcePacking.CanAdd(used, new Resources(3, 2), capacity), "CPU limit despite spare memory");
        Check(!MultiResourcePacking.CanAdd(used, new Resources(1, 13), capacity), "memory limit despite spare CPU");
        Check(MultiResourcePacking.CanAdd(used, new Resources(2, 12), capacity), "both resource limits exactly met");
        Check(!MultiResourcePacking.CanAdd(new Resources(9, 0), new Resources(0, 0), capacity), "already overloaded resource");
        Throws(() => MultiResourcePacking.CanAdd(used, new Resources(-1, 2), capacity), "negative resource demand");

        var container = new Box3D(6, 4, 4);
        var lower = new PlacedBox(new Box3D(4, 4, 2), new Point3D(0, 0, 0));
        var right = new PlacedBox(new Box3D(2, 4, 4), new Point3D(4, 0, 0));
        var upper = new PlacedBox(new Box3D(4, 4, 2), new Point3D(0, 0, 2));
        Check(BoxGeometry.CanPlace(container, lower, Array.Empty<PlacedBox>()), "first box at origin");
        Check(!BoxGeometry.FitsInside(container, new PlacedBox(new Box3D(4, 4, 2), new Point3D(4, 0, 0))), "unrotated box exceeds container width");
        Check(BoxGeometry.CanPlace(container, right, new[] { lower }), "rotated box fits beside first");
        Check(BoxGeometry.CanPlace(container, upper, new[] { lower, right }), "last box fills top space");
        Check(!BoxGeometry.Overlaps(lower, right) && !BoxGeometry.Overlaps(lower, upper), "touching faces are allowed");
        Check(BoxGeometry.Overlaps(lower, new PlacedBox(new Box3D(1, 1, 1), new Point3D(1, 1, 1))), "contained box overlaps");
        Check(!BoxGeometry.FitsInside(container, lower with { Position = new Point3D(-1, 0, 0) }), "negative placement is outside");
        Check(BoxGeometry.Orientations(new Box3D(2, 3, 4)).Count() == 6, "six distinct orthogonal orientations");
        Check(BoxGeometry.Orientations(new Box3D(4, 4, 2)).Count() == 3, "symmetric orientations deduplicated");
        Throws(() => BoxGeometry.Orientations(new Box3D(0, 1, 1)), "zero-size box");
        var sphere = new Sphere(0, 0, 0, 1);
        Check(!SphereGeometry.Overlaps(sphere, new Sphere(2, 0, 0, 1)), "touching spheres");
        Check(SphereGeometry.Overlaps(sphere, new Sphere(1.9, 0, 0, 1)), "intersecting spheres");
        Check(!SphereGeometry.Overlaps(sphere, new Sphere(1.5, 1.5, 0, 1)), "overlapping bounds do not imply overlapping spheres");
        Throws(() => SphereGeometry.Overlaps(sphere, new Sphere(0, 0, 0, -1)), "negative sphere radius");
        Throws(() => SphereGeometry.Overlaps(sphere, new Sphere(double.NaN, 0, 0, 1)), "non-finite sphere coordinate");
        var shapeA = new HashSet<(int X, int Y)> { (0, 0), (0, 1), (1, 1) };
        var shapeB = new HashSet<(int X, int Y)> { (1, 0), (2, 0), (2, 1) };
        Check(!shapeA.Overlaps(shapeB), "interlocking L shapes have disjoint occupied cells");
        Check(shapeA.Concat(shapeB).Distinct().Count() == 6, "L shapes fill a three-by-two sheet");

        // Independent minute-by-minute oracle: the sweep must return exactly the free runs.
        var random = new Random(42);
        for (var trial = 0; trial < 500; trial++)
        {
            var occupied = Enumerable.Range(0, random.Next(15)).Select(_ =>
            {
                var start = random.Next(-20, 140);
                return new TimeRange(start, start + random.Next(1, 40));
            }).ToArray();
            var minimum = random.Next(1, 35);
            var expected = new List<TimeRange>();
            int? run = null;
            for (var minute = 0; minute <= 120; minute++)
            {
                var free = minute < 120 && !occupied.Any(b => b.Start <= minute && minute < b.End);
                if (free && run is null) run = minute;
                if (!free && run is { } start)
                {
                    if (minute - start >= minimum) expected.Add(new TimeRange(start, minute));
                    run = null;
                }
            }
            Check(Availability.FindGaps(new TimeRange(0, 120), occupied, minimum).SequenceEqual(expected), $"gap oracle trial {trial}");
            var items = Enumerable.Range(0, random.Next(30)).Select(_ => random.Next(1, 11)).ToArray();
            foreach (var bins in new[] { BinPacking.FirstFit(items, 10), BinPacking.BestFit(items, 10) })
            {
                Check(bins.All(bin => bin.Sum() <= 10), "packing respects capacity");
                Check(bins.SelectMany(bin => bin).Order().SequenceEqual(items.Order()), "packing preserves every item");
            }
        }
        Console.WriteLine($"Passed {_count} checks, including 500 generated calendar and packing scenarios.");
    }

    private static void Check(bool condition, string name)
    {
        _count++;
        if (!condition) throw new InvalidOperationException("Failed: " + name);
    }
    private static void Throws(Action action, string name)
    {
        try { action(); }
        catch (ArgumentException) { Check(true, name); return; }
        throw new InvalidOperationException("Expected argument failure: " + name);
    }
}
