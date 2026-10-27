using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Mostlylucid.Relevance;

// Parse a bounded syntax tree, then use generated JSON metadata. No reflected POCO discovery.
internal static class YamlConfiguration
{
    public static RelevanceDefinition Read(string yaml)
    {
        if (Encoding.UTF8.GetByteCount(yaml) > 1_048_576) throw new ArgumentException("Configuration exceeds 1 MiB.");
        var parser = new Parser(new StringReader(yaml));
        var depth = 0;
        var nodes = 0;
        while (parser.MoveNext())
        {
            if (++nodes > 100_000) throw new ArgumentException("Too many YAML nodes.");
            if (parser.Current is AnchorAlias) throw new ArgumentException("YAML aliases are not supported; use explicit configuration.");
            if (parser.Current is MappingStart or SequenceStart && ++depth > 32) throw new ArgumentException("YAML nesting exceeds 32 levels.");
            if (parser.Current is MappingEnd or SequenceEnd) depth--;
        }
        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));
        if (stream.Documents.Count != 1) throw new ArgumentException("Provide exactly one YAML configuration document.");
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) Write(writer, stream.Documents[0].RootNode);
        var options = new JsonSerializerOptions(CoreJsonContext.Default.Options)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        return JsonSerializer.Deserialize(buffer.ToArray(), new CoreJsonContext(options).RelevanceDefinition)
            ?? throw new ArgumentException("Empty configuration.");
    }
    private static void Write(Utf8JsonWriter writer, YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                writer.WriteStartObject();
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var (key, value) in mapping.Children)
                {
                    if (key is not YamlScalarNode { Value: { } name } || !names.Add(name))
                        throw new ArgumentException("YAML keys must be unique strings.");
                    writer.WritePropertyName(name);
                    Write(writer, value);
                }
                writer.WriteEndObject();
                break;
            case YamlSequenceNode sequence:
                writer.WriteStartArray();
                foreach (var child in sequence.Children) Write(writer, child);
                writer.WriteEndArray();
                break;
            case YamlScalarNode scalar:
                var text = scalar.Value ?? "";
                if (scalar.Style is not (ScalarStyle.Plain or ScalarStyle.Any) || scalar.Tag.ToString() == "tag:yaml.org,2002:str") writer.WriteStringValue(text);
                else if (text is "" or "~" or "null" or "Null" or "NULL") writer.WriteNullValue();
                else if (bool.TryParse(text, out var boolean)) writer.WriteBooleanValue(boolean);
                else if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)) writer.WriteNumberValue(integer);
                else if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)) writer.WriteNumberValue(number);
                else writer.WriteStringValue(text);
                break;
            default: throw new ArgumentException("Unsupported YAML node.");
        }
    }
}
