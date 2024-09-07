namespace Umami.Net.Helpers;

using System.IdentityModel.Tokens.Jwt;

public static class JwtDecoder
{
    public static  async Task<JwtPayload?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var content =await responseMessage.Content.ReadAsStringAsync();
        return DecodeJwt(content);
    }

    private static JwtPayload? DecodeJwt(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
        var json = jsonToken?.Payload;
        return json;


    }
}