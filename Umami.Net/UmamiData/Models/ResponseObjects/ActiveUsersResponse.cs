using System.Text.Json.Serialization;

namespace Umami.Net.UmamiData.Models.ResponseObjects;

public class ActiveUsersResponse
{
    [JsonPropertyName("x")]
    public int ActiveUsers { get; set; }
}