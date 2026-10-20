using System.Text.Json.Serialization;

namespace Umami.Net.UmamiData.Models.ResponseObjects;

public class ActiveUsersResponse
{
    [JsonPropertyName("visitors")] public int ActiveUsers { get; set; }
    // Read legacy responses without changing the public ActiveUsers property.
    [JsonPropertyName("x"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyCount { get => null; set { if (value.HasValue) ActiveUsers = value.Value; } }
}