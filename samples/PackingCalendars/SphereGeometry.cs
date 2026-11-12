namespace PackingCalendars;

public sealed record Sphere(double X, double Y, double Z, double Radius);

public static class SphereGeometry
{
    public static bool Overlaps(Sphere a, Sphere b)
    {
        Validate(a);
        Validate(b);
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        var radii = a.Radius + b.Radius;
        return dx * dx + dy * dy + dz * dz < radii * radii;
    }

    private static void Validate(Sphere sphere)
    {
        if (!double.IsFinite(sphere.X) || !double.IsFinite(sphere.Y) || !double.IsFinite(sphere.Z) ||
            !double.IsFinite(sphere.Radius) || sphere.Radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(sphere), "Sphere values must be finite and radius positive.");
    }
}
