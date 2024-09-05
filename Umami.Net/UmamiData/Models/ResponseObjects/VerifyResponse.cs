using System.Text.Json.Serialization;

namespace Umami.Net.UmamiData.Models.ResponseObjects;

public class VerifyResponse
{

    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; }
    [JsonPropertyName("role")]
    public string Role { get; set; }
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }


 
}