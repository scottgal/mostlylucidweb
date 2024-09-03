using System.Net;
using System.Net.Http.Json;
using System.Text;
using Umami.Net.Models;
using Umami.Net.Test.Extensions;

namespace Umami.Net.Test.MessageHandlers;

public class JwtResponseHandler
{
    public static HttpMessageHandler Create()
    {
        var handler = EchoMockHandler.Create(async (request, cancellationToken) =>
        {
            var umamiPayload =await request.Content?.ReadFromJsonAsync<EchoedRequest>(cancellationToken: cancellationToken)!;
            if(umamiPayload == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
           var  responseContent = JwtExtensions.GenerateJwt(umamiPayload.Payload);
            
            // Return the response
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };
        });
        return handler;
    }
}