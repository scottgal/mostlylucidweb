using System.Net;
using System.Net.Http.Json;
using System.Text;
using Moq;
using Moq.Protected;
using Umami.Net.Models;
using Umami.Net.Test.Extensions;

namespace Umami.Net.Test.MessageHandlers;

public class JwtResponseHandler
{
    public static HttpMessageHandler Create()
    {
        var handler = EchoMockHandler.Create(async (request, cancellationToken) =>
        {
            // Read the request content
            var requestBody = request.Content != null
                ? request.Content.ReadAsStringAsync(cancellationToken).Result
                : null;

            // Create a response that echoes the request body
         

            var userAgent = request.Headers.UserAgent.ToString();
            var umamiPayload =await request.Content?.ReadFromJsonAsync<UmamiPayload>();
           var  responseContent = JwtExtensions.GenerateJwt(umamiPayload);
            
            // Return the response
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };
        });
        return handler;
    }
}