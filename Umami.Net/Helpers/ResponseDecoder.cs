using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

namespace Umami.Net.Helpers;

public class JwtDecoder(ILogger<JwtDecoder> logger)
{
    public async Task<JwtPayload?> DecodeResponse(string content)
    {
        try
        {
            return DecodeJwt(content);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to read content");
            return null;
        }
    }

    private JwtPayload? DecodeJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
            var json = jsonToken?.Payload;
            return json;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to decode JWT for {Token}", token);
            return null;
        }
    }
}