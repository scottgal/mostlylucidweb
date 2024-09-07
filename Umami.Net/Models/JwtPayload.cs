using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

using System;

public class UmamiResponse
{
public static UmamiResponse Decode(JwtPayload payload)
{
    payload.TryGetValue("visitId", out object? visitIdObj);
    payload.TryGetValue("iat", out object? iatObj);
    //This should only happen then the payload is dummy.
    if (payload.Count == 2)
    {
       

        Guid visitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty;
        long iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0;

        return new UmamiResponse()
        {
            VisitId = visitId,
            Iat = iat
        };
    }
    else
    {
        payload.TryGetValue("id", out object? idObj);
        payload.TryGetValue("websiteId", out object? websiteIdObj);
        payload.TryGetValue("hostname", out object? hostnameObj);
        payload.TryGetValue("browser", out object? browserObj);
        payload.TryGetValue("os", out object? osObj);
        payload.TryGetValue("device", out object? deviceObj);
        payload.TryGetValue("screen", out object? screenObj);
        payload.TryGetValue("language", out object? languageObj);
        payload.TryGetValue("country", out object? countryObj);
        payload.TryGetValue("subdivision1", out object? subdivision1Obj);
        payload.TryGetValue("subdivision2", out object? subdivision2Obj);
        payload.TryGetValue("city", out object? cityObj);
        payload.TryGetValue("createdAt", out object? createdAtObj);

        return new UmamiResponse()
        {
            Id = idObj != null ? Guid.Parse(idObj.ToString()!) : Guid.Empty,
            WebsiteId = websiteIdObj != null ? Guid.Parse(websiteIdObj.ToString()!) : Guid.Empty,
            Hostname = hostnameObj?.ToString(),
            Browser = browserObj?.ToString(),
            Os = osObj?.ToString(),
            Device = deviceObj?.ToString(),
            Screen = screenObj?.ToString(),
            Language = languageObj?.ToString(),
            Country = countryObj?.ToString(),
            Subdivision1 = subdivision1Obj?.ToString(),
            Subdivision2 = subdivision2Obj?.ToString(),
            City = cityObj?.ToString(),
            CreatedAt = createdAtObj != null ? DateTime.Parse(createdAtObj.ToString()!) : DateTime.MinValue,
            VisitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty,
            Iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0
        };
    }
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