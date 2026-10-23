using System.Text.Json;

namespace Mostlylucid.Relevance;

/// <summary>Reference exact distances and portable float32 storage for small vector spaces.</summary>
public static class VectorMath
{
    /// <summary>Rejects empty, incorrectly sized or non-finite vectors.</summary>
    public static void Validate(float[] vector, int dimensions)
    {
        if (vector.Length != dimensions || dimensions < 1 || vector.Any(v => !float.IsFinite(v)))
            throw new ArgumentException("Vector must have the declared dimension and finite coordinates.");
    }

    /// <summary>Computes L2 or cosine distance between equal-length finite vectors; cosine requires nonzero vectors.</summary>
    public static double Distance(float[] a, float[] b, string metric)
    {
        Validate(a, b.Length);
        Validate(b, a.Length);
        double dot = 0, aa = 0, bb = 0, squared = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var x = (double)a[i];
            var y = (double)b[i];
            dot += x * y;
            aa += x * x;
            bb += y * y;
            squared += (x - y) * (x - y);
        }
        return metric switch
        {
            "l2" => Math.Sqrt(squared),
            "cosine" when aa > 0 && bb > 0 => Math.Clamp(1 - dot / Math.Sqrt(aa * bb), 0, 2),
            "cosine" => throw new ArgumentException("Cosine distance requires nonzero vectors."),
            _ => throw new ArgumentException($"Unknown metric '{metric}'.")
        };
    }

    /// <summary>Encodes coordinates as little-endian float32 bytes compatible with sqlite-vec BLOBs.</summary>
    public static byte[] Encode(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        for (var i = 0; i < vector.Length; i++) System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), vector[i]);
        return bytes;
    }

    /// <summary>Decodes little-endian float32 bytes, rejecting partial coordinates; validate finiteness separately.</summary>
    public static float[] Decode(byte[] bytes)
    {
        if (bytes.Length % 4 != 0) throw new ArgumentException("Invalid float32 vector bytes.");
        var vector = new float[bytes.Length / 4];
        for (var i = 0; i < vector.Length; i++) vector[i] = System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * 4));
        return vector;
    }
}

/// <summary>Maps a number to one coordinate by dividing by the configured positive scale.</summary>
public sealed class NumericConverter : IPointConverter
{
    /// <inheritdoc />
    public string Name => "numeric";
    /// <inheritdoc />
    public string Version => "1";
    /// <inheritdoc />
    public int Dimensions(PointDefinition point) => 1;
    /// <inheritdoc />
    public ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default) => ValueTask.FromResult(new[] { (float)(value.GetDouble() / point.Scale) });
}

/// <summary>One-hot encodes a string using an explicit ordered vocabulary; rejects unknown categories.</summary>
public sealed class CategoricalConverter : IPointConverter
{
    /// <inheritdoc />
    public string Name => "categorical";
    /// <inheritdoc />
    public string Version => "1";
    /// <inheritdoc />
    public int Dimensions(PointDefinition point)
    {
        if (point.Categories.Count == 0 || point.Categories.Distinct(StringComparer.Ordinal).Count() != point.Categories.Count)
            throw new ArgumentException("Categorical points require distinct, ordered categories.");
        return point.Categories.Count;
    }
    /// <inheritdoc />
    public ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default)
    {
        var index = point.Categories.IndexOf(value.GetString() ?? "");
        if (index < 0) throw new ArgumentException("Unknown category; extend the versioned vocabulary explicitly.");
        var result = new float[Dimensions(point)];
        result[index] = 1;
        return ValueTask.FromResult(result);
    }
}

/// <summary>Reads already encoded coordinates; the engine verifies declared dimensions and finite values.</summary>
public sealed class PrecomputedConverter : IPointConverter
{
    /// <inheritdoc />
    public string Name => "vector";
    /// <inheritdoc />
    public string Version => "1";
    /// <inheritdoc />
    public int Dimensions(PointDefinition point) => point.Dimensions > 0 ? point.Dimensions : throw new ArgumentException("Declare precomputed vector dimensions.");
    /// <inheritdoc />
    public ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default) => ValueTask.FromResult(value.EnumerateArray().Select(v => v.GetSingle()).ToArray());
}
