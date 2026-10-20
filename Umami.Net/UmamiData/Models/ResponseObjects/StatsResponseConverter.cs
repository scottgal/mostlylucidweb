using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umami.Net.UmamiData.Models.ResponseObjects;

public sealed class StatsResponseConverter : JsonConverter<StatsResponseModels>
{
    public override StatsResponseModels Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        (int Value, int Previous) ReadMetric(string name)
        {
            if (!root.TryGetProperty(name, out var metric)) return (0, 0);
            if (metric.ValueKind == JsonValueKind.Object)
                return (metric.GetProperty("value").GetInt32(), metric.TryGetProperty("prev", out var prev) ? prev.GetInt32() : 0);
            return (metric.GetInt32(), root.TryGetProperty("comparison", out var comparison) && comparison.TryGetProperty(name, out var previous)
                ? previous.GetInt32() : 0);
        }
        var p = ReadMetric("pageviews"); var v = ReadMetric("visitors"); var visits = ReadMetric("visits");
        var b = ReadMetric("bounces"); var t = ReadMetric("totaltime");
        return new()
        {
            pageviews = new() { value = p.Value, prev = p.Previous }, visitors = new() { value = v.Value, prev = v.Previous },
            visits = new() { value = visits.Value, prev = visits.Previous }, bounces = new() { value = b.Value, prev = b.Previous },
            totaltime = new() { value = t.Value, prev = t.Previous }
        };
    }

    public override void Write(Utf8JsonWriter writer, StatsResponseModels value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("pageviews"); JsonSerializer.Serialize(writer, value.pageviews, options);
        writer.WritePropertyName("visitors"); JsonSerializer.Serialize(writer, value.visitors, options);
        writer.WritePropertyName("visits"); JsonSerializer.Serialize(writer, value.visits, options);
        writer.WritePropertyName("bounces"); JsonSerializer.Serialize(writer, value.bounces, options);
        writer.WritePropertyName("totaltime"); JsonSerializer.Serialize(writer, value.totaltime, options);
        writer.WriteEndObject();
    }
}
