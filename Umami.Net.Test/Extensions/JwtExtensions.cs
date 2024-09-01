using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Umami.Net.Models;
using JwtPayload = System.IdentityModel.Tokens.Jwt.JwtPayload;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Umami.Net.Test.Extensions;

public static class JwtExtensions
{
    private const string Secret = "2B8758EA-8E59-49C9-9005-F1117AC24168";
    public static string GenerateJwt( UmamiPayload umamiPayload, string secret = Secret)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
//Based on website_id, hostname, ip, userAgent
        var visitorId =
            $"{umamiPayload.Website}{umamiPayload.Hostname}{umamiPayload.IpAddress}{umamiPayload.UserAgent}";
        var claims = new[]
        {
            new Claim("id", "28ac27ad-9945-52b1-a629-7004f174a644"),
            new Claim("websiteId", umamiPayload.Website),
            new Claim("hostname", umamiPayload.Hostname),
            new Claim("browser", "chrome"),
            new Claim("os", "Windows 10"),
            new Claim("device", ""),
            new Claim("screen", ""),
            new Claim("language", ""),
            new Claim("country", "GB"),
            new Claim("createdAt", "2024-08-30T10:19:28.462Z"),
            new Claim("visitId", visitorId),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            header: new JwtHeader(credentials),
            payload: new JwtPayload(claims)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public static string DecodeJwt(string token, string secret = Secret)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

        var jwtToken = validatedToken as JwtSecurityToken;
        if (jwtToken == null)
        {
            throw new Exception("Token is not a valid JWT.");
        }

        // Extract payload claims
        var payload = new StringBuilder();
        foreach (var claim in jwtToken.Claims)
        {
            payload.AppendLine($"{claim.Type}: {claim.Value}");
        }

        return payload.ToString();
    }
    
}