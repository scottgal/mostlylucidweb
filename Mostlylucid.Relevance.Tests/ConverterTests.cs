using System.Text.Json;

namespace Mostlylucid.Relevance.Tests;

public sealed class ConverterTests
{
    [Theory]
    [InlineData("clamp((x - 20) / 180, 0, 1)", 110, 0.5)]
    [InlineData("max(0, min(x, 10))", 20, 10)]
    [InlineData("abs(-x) + 2 * 3", 4, 10)]
    [InlineData("1e-2 * x", 50, 0.5)]
    public async Task FormulaGrammarHasPredictableArithmetic(string expression, double input, double expected)
    {
        var point = new PointDefinition { Expression = expression };
        var converter = new FormulaConverter();
        Assert.Equal(1, converter.Dimensions(point));
        var result = await converter.ConvertAsync(JsonSerializer.SerializeToElement(input), point);
        Assert.Equal(expected, result[0], 5);
    }

    [Theory]
    [InlineData("System.IO.File.ReadAllText(x)")]
    [InlineData("x; 5")]
    [InlineData("x +")]
    [InlineData("max(x)")]
    [InlineData("1e999")]
    public void InvalidOrExecutableExpressionsAreRejectedAtConfiguration(string expression)
    {
        Assert.Throws<ArgumentException>(() => new FormulaConverter().Dimensions(new PointDefinition { Expression = expression }));
    }

    [Fact]
    public async Task DivisionByZeroAndOverflowCannotBecomeVectorCoordinates()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await new FormulaConverter().ConvertAsync(
            JsonSerializer.SerializeToElement(1), new PointDefinition { Expression = "x / 0" }));
        await Assert.ThrowsAsync<ArgumentException>(async () => await new FormulaConverter().ConvertAsync(
            JsonSerializer.SerializeToElement(1e30), new PointDefinition { Expression = "x * 1e30" }));
    }

    [Fact]
    public async Task OklabUsesMeasuredSwatchesAndPreservesApproximateShadeDistances()
    {
        var converter = new OklabConverter();
        var point = new PointDefinition { Metric = "l2" };
        Assert.Equal(3, converter.Dimensions(point));
        var black = await converter.ConvertAsync(JsonSerializer.SerializeToElement("#000000"), point);
        var white = await converter.ConvertAsync(JsonSerializer.SerializeToElement("#ffffff"), point);
        var red = await converter.ConvertAsync(JsonSerializer.SerializeToElement("#ff0000"), point);
        var nearRed = await converter.ConvertAsync(JsonSerializer.SerializeToElement("#fa0000"), point);
        var blue = await converter.ConvertAsync(JsonSerializer.SerializeToElement("#0000ff"), point);
        Assert.Equal(new float[] { 0, 0, 0 }, black);
        Assert.Equal(1.0, white[0], 5);
        Assert.Equal(0.6279554, red[0], 5);
        Assert.True(VectorMath.Distance(red, nearRed, "l2") < VectorMath.Distance(red, blue, "l2"));
        await Assert.ThrowsAsync<ArgumentException>(async () => await converter.ConvertAsync(JsonSerializer.SerializeToElement("russet"), point));
    }
}
