using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.Test.UmamiData;

public class UmamiDataDelegatingHandler : DelegatingHandler
{
    private record AuthRequest(string username, string password);
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestContent = await request.Content.ReadFromJsonAsync<AuthRequest> (cancellationToken);
       switch(request.RequestUri.AbsolutePath)
       {
           case "/api/auth/login" when requestContent?.username == "username" && requestContent?.password == "password":
               var authResponse = new AuthResponse()
               {
                   Token = "1234567890",
                   User = new UserResponse()
                   {
                       Id = "123",
                       Username = "test",
                       Role = "admin",
                       CreatedAt = DateTime.Now,
                       IsAdmin = true
                   }

               };
               var json = JsonSerializer.Serialize(authResponse);
               return new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new StringContent(json, Encoding.UTF8, "application/json")
               };
    
           case "/api/auth/login" when requestContent?.username == "bad":
               return new HttpResponseMessage(HttpStatusCode.Unauthorized);
           default:
               return new HttpResponseMessage(HttpStatusCode.NotFound);
       }
    }
}