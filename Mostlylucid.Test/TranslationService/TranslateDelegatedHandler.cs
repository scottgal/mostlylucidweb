using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Mostlylucid.Test.TranslationService.Helpers;
using Mostlylucid.Test.TranslationService.Models;

namespace Mostlylucid.Test.TranslationService;

public class TranslateDelegatedHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri.Host;
        var method= request.Method;
        var absPath = request.RequestUri.AbsolutePath;
        switch (absPath)
        {
            case "/translate" when method == HttpMethod.Post:
                return await TranslateResponses(request);
            case "/translate":
                return new HttpResponseMessage(HttpStatusCode.OK);
            case "/model_name" when host == Consts.GoodHost:
                return new HttpResponseMessage(HttpStatusCode.OK);
            case "/model_name" when host == Consts.BadHost:
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static async Task<HttpResponseMessage> TranslateResponses(HttpRequestMessage request)
    {
        var contentRequest = await request.Content.ReadFromJsonAsync<PostRecord>();
        
        
        if(contentRequest.target_lang == "xx")
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                
        var postResponse= new PostResponse("es", new[] {"Esto es una prueba"}, "en", 0.1f);
        var content= new StringContent(JsonSerializer.Serialize(postResponse), Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = content;
        return response;
    }
}