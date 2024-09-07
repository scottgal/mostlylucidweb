using Microsoft.Extensions.Logging;

namespace Umami.Net.Helpers;

using System.IdentityModel.Tokens.Jwt;

public  class JwtDecoder(ILogger<JwtDecoder> logger)
{
    public  async Task<JwtPayload?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        try
        {

       
         var content = await responseMessage.Content.ReadAsStringAsync();
         return DecodeJwt(content);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to decode response");
            return null;
        }
    }

    private  JwtPayload? DecodeJwt(string token)
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