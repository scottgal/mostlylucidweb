namespace PackingCalendars;

public sealed record Box3D(int Width, int Depth, int Height);
public sealed record Point3D(int X, int Y, int Z);
public sealed record PlacedBox(Box3D Size, Point3D Position);

// Axis-aligned geometry only: a loading plan also needs weight and support rules.
public static class BoxGeometry
{
    public static IEnumerable<Box3D> Orientations(Box3D box)
    {
        Validate(box);
        var w = box.Width;
        var d = box.Depth;
        var h = box.Height;
        return new[]
        {
            new Box3D(w, d, h), new Box3D(w, h, d),
            new Box3D(d, w, h), new Box3D(d, h, w),
            new Box3D(h, w, d), new Box3D(h, d, w)
        }.Distinct();
    }

    public static bool FitsInside(Box3D container, PlacedBox item)
    {
        Validate(container);
        Validate(item.Size);
        var p = item.Position;
        return p.X >= 0 && p.Y >= 0 && p.Z >= 0 &&
               item.Size.Width <= container.Width - p.X &&
               item.Size.Depth <= container.Depth - p.Y &&
               item.Size.Height <= container.Height - p.Z;
    }

    public static bool Overlaps(PlacedBox a, PlacedBox b)
    {
        Validate(a.Size);
        Validate(b.Size);
        // Use long for interval ends so adding two int values cannot wrap around.
        return a.Position.X < (long)b.Position.X + b.Size.Width &&
               b.Position.X < (long)a.Position.X + a.Size.Width &&
               a.Position.Y < (long)b.Position.Y + b.Size.Depth &&
               b.Position.Y < (long)a.Position.Y + a.Size.Depth &&
               a.Position.Z < (long)b.Position.Z + b.Size.Height &&
               b.Position.Z < (long)a.Position.Z + a.Size.Height;
    }

    public static bool CanPlace(Box3D container, PlacedBox candidate, IEnumerable<PlacedBox> placed) =>
        FitsInside(container, candidate) && !placed.Any(item => Overlaps(item, candidate));

    private static void Validate(Box3D box)
    {
        if (box.Width <= 0 || box.Depth <= 0 || box.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(box), "Box dimensions must be positive.");
    }
}
