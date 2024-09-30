using System.Text;

namespace Mostlylucid.EmailSubscription;

public class NewsletterClient(HttpClient client)
{
    public async Task SendNewsletter(string token)
    {
        var clientCall = new HttpRequestMessage(HttpMethod.Get, "api/sendfortoken");
        clientCall.Content = new StringContent(token, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(clientCall);
        
    }
    
}