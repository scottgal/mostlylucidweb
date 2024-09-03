using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

using System;

public class UmamiResponse
{
    public static UmamiResponse Decode(JwtPayload payload)
    {
        return new UmamiResponse
        {
            Id = Guid.Parse(payload["id"].ToString()!),
            WebsiteId = Guid.Parse(payload["websiteId"].ToString()!),
            Hostname = payload["hostname"]?.ToString(),
            Browser = payload["browser"]?.ToString(),
            Os = payload["os"]?.ToString(),
            Device = payload["device"]?.ToString(),
            Screen = payload["screen"]?.ToString(),
            Language = payload["language"]?.ToString(),
            Country = payload["country"]?.ToString(),
            Subdivision1 = payload["subdivision1"]?.ToString(),
            Subdivision2 = payload["subdivision2"]?.ToString(),
            City = payload["city"]?.ToString(),
            CreatedAt = DateTime.Parse(payload["createdAt"]?.ToString()!),
            VisitId = Guid.Parse(payload["visitId"].ToString()!),
            Iat = long.Parse(payload["iat"].ToString()!)
        };
    }
    
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }
}