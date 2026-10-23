using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mostlylucid.Relevance;

// A bounded scalar expression grammar, not a scripting runtime. No reflection, IO or user code evaluation.
/// <summary>Evaluates bounded numeric expressions over x without scripting, reflection or I/O.</summary>
public sealed class FormulaConverter : IPointConverter
{
    private readonly ConcurrentDictionary<string, Func<double, double>> expressions = new(StringComparer.Ordinal);
    /// <inheritdoc />
    public string Name => "formula";
    /// <inheritdoc />
    public string Version => "1";
    /// <inheritdoc />
    public int Dimensions(PointDefinition point) { Compile(point.Expression); return 1; }
    /// <inheritdoc />
    public ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default)
    {
        var result = Compile(point.Expression)(Finite(value.GetDouble()));
        var coordinate = (float)result;
        if (!float.IsFinite(coordinate)) throw new ArgumentException("Formula result is outside float32 range.");
        return ValueTask.FromResult(new[] { coordinate });
    }
    private Func<double, double> Compile(string expression) => expressions.GetOrAdd(expression, e => new Parser(e).Parse());
    private static double Finite(double value) => double.IsFinite(value) ? value : throw new ArgumentException("Formula produced a non-finite value.");

    private sealed class Parser
    {
        private static readonly Regex Lexer = new(@"\G\s*(?:(?<number>(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)|(?<name>[A-Za-z_][A-Za-z0-9_]*)|(?<operator>[+*/(),-]))", RegexOptions.CultureInvariant);
        private readonly List<string> tokens = [];
        private int position;
        private int depth;
        public Parser(string expression)
        {
            expression = expression.Trim();
            if (expression.Length is < 1 or > 256) throw new ArgumentException("Formula must contain 1 to 256 characters.");
            var offset = 0;
            while (offset < expression.Length)
            {
                var match = Lexer.Match(expression, offset);
                if (!match.Success || tokens.Count >= 128) throw new ArgumentException("Invalid or over-complex formula.");
                tokens.Add(match.Groups["number"].Success ? match.Groups["number"].Value : match.Groups["name"].Success ? match.Groups["name"].Value : match.Groups["operator"].Value);
                offset += match.Length;
            }
        }
        public Func<double, double> Parse()
        {
            var expression = Add();
            if (position != tokens.Count) throw new ArgumentException("Unexpected formula token.");
            return expression;
        }
        private bool Take(string token)
        {
            if (position >= tokens.Count || tokens[position] != token) return false;
            position++;
            return true;
        }
        private void Require(string token) { if (!Take(token)) throw new ArgumentException($"Expected '{token}' in formula."); }
        private Func<double, double> Add()
        {
            var node = Multiply();
            while (position < tokens.Count && tokens[position] is "+" or "-")
            {
                var op = tokens[position++];
                var left = node;
                var right = Multiply();
                node = x => Finite(op == "+" ? left(x) + right(x) : left(x) - right(x));
            }
            return node;
        }
        private Func<double, double> Multiply()
        {
            var node = Primary();
            while (position < tokens.Count && tokens[position] is "*" or "/")
            {
                var op = tokens[position++];
                var left = node;
                var right = Primary();
                node = x => Finite(op == "*" ? left(x) * right(x) : right(x) == 0
                    ? throw new ArgumentException("Division by zero in formula.") : left(x) / right(x));
            }
            return node;
        }
        private Func<double, double> Primary()
        {
            if (++depth > 16) throw new ArgumentException("Formula nesting exceeds 16 levels.");
            try
            {
                if (Take("-")) { var child = Primary(); return x => -child(x); }
                if (Take("+")) return Primary();
                if (Take("(")) { var child = Add(); Require(")"); return child; }
                if (position >= tokens.Count) throw new ArgumentException("Incomplete formula.");
                var token = tokens[position++];
                if (token == "x") return x => x;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) { Finite(number); return _ => number; }
                if (token is not ("min" or "max" or "clamp" or "abs")) throw new ArgumentException($"Unknown formula name '{token}'.");
                Require("(");
                var a = Add();
                if (token == "abs") { Require(")"); return x => Math.Abs(a(x)); }
                Require(",");
                var b = Add();
                if (token == "clamp") { Require(","); var c = Add(); Require(")"); return x => Math.Clamp(a(x), b(x), c(x)); }
                Require(")");
                return token == "min" ? x => Math.Min(a(x), b(x)) : x => Math.Max(a(x), b(x));
            }
            finally { depth--; }
        }
    }
}

/// <summary>Encodes a boolean as one coordinate: false is zero, true is one.</summary>
public sealed class BooleanConverter : IPointConverter
{
    /// <inheritdoc />
    public string Name => "boolean";
    /// <inheritdoc />
    public string Version => "1";
    /// <inheritdoc />
    public int Dimensions(PointDefinition point) => 1;
    /// <inheritdoc />
    public ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default) => ValueTask.FromResult(new[] { value.GetBoolean() ? 1f : 0f });
}

/// <summary>Converts a measured #RRGGBB sRGB swatch to Oklab for approximate perceptual L2 comparison.</summary>
public sealed class OklabConverter : IPointConverter
{
    /// <inheritdoc />
    public string Name => "rgb-oklab";
    /// <inheritdoc />
    public string Version => "1";
    /// <inheritdoc />
    public int Dimensions(PointDefinition point)
    {
        if (point.Metric != "l2") throw new ArgumentException("Oklab points use l2 distance, not cosine.");
        return 3;
    }
    /// <inheritdoc />
    public ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default)
    {
        var hex = value.GetString() ?? "";
        if (!Regex.IsMatch(hex, @"\A#[0-9a-fA-F]{6}\z")) throw new ArgumentException("Colour must be an sRGB #RRGGBB swatch, not a colour name.");
        double Channel(int start)
        {
            var v = int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        var r = Channel(1); var g = Channel(3); var b = Channel(5);
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
        return ValueTask.FromResult(new[] { (float)(0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s),
            (float)(1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s), (float)(0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s) });
    }
}
