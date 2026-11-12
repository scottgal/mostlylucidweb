using System.Text.Json;
using PackingCalendars;

namespace ResourcePacking.Examples;

public sealed record BoxItem(string Id, Box3D Size, bool Required = false);
public sealed record BoxCandidates(PackingProblem Problem, IReadOnlyDictionary<string, PlacedBox> Placements);

public static class BoxPacking
{
    public static BoxCandidates Generate(Box3D container, IReadOnlyList<BoxItem> items,
        IReadOnlyList<Point3D> anchors, int candidateLimit = 1_000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(candidateLimit);
        _ = BoxGeometry.Orientations(container);
        var positions = anchors.Distinct().ToArray();
        var placements = new Dictionary<string, PlacedBox>(StringComparer.Ordinal);
        var candidates = new List<Candidate>();
        foreach (var item in items)
        foreach (var orientation in BoxGeometry.Orientations(item.Size))
        foreach (var anchor in positions)
        {
            var placement = new PlacedBox(orientation, anchor);
            if (!BoxGeometry.FitsInside(container, placement)) continue;
            var id = JsonSerializer.Serialize(new
            {
                item.Id, anchor.X, anchor.Y, anchor.Z, orientation.Width, orientation.Depth, orientation.Height
            });
            if (placements.Count == candidateLimit)
                throw new ArgumentException("The candidate limit was exceeded. Use fewer anchors or a specialised packer.");
            placements.Add(id, placement);
            candidates.Add(new Candidate(id, item.Id, [], []));
        }

        var conflicts = new List<CandidateConflict>();
        for (var first = 0; first < candidates.Count; first++)
        for (var second = first + 1; second < candidates.Count; second++)
        {
            var a = candidates[first];
            var b = candidates[second];
            if (a.RequestId != b.RequestId && BoxGeometry.Overlaps(placements[a.Id], placements[b.Id]))
                conflicts.Add(new CandidateConflict(a.Id, b.Id));
        }

        return new BoxCandidates(new PackingProblem([], items.Select(item => new Request(item.Id, item.Required)),
            candidates, candidatesComplete: false, conflicts: conflicts), placements.AsReadOnly());
    }

    public static void Verify(Box3D container, BoxCandidates input, PackingResult result)
    {
        PackingVerifier.Verify(input.Problem, result.Selected, result.Status, result.CountProvenOptimal, result.Elapsed);
        var placed = new List<PlacedBox>();
        foreach (var id in result.Selected)
        {
            var box = input.Placements[id];
            if (!BoxGeometry.CanPlace(container, box, placed))
                throw new InvalidOperationException("The physical layout failed geometry verification.");
            placed.Add(box);
        }
    }
}
