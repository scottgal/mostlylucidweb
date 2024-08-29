
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.Helpers;
using Umami.Net.Models;

namespace Umami.Net
{
   public class UmamiClient(
       HttpClient client,
       PayloadService payloadService,
       ILogger<UmamiClient> logger,
       UmamiClientSettings settings)
   {


    private static  JsonSerializerOptions options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = new LowerCaseNamingPolicy(), // Custom naming policy for lower-cased properties
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
        public async Task<HttpResponseMessage> Send(
            UmamiPayload? payload=null,
            UmamiEventData? eventData =null,
            string type = "event")
        {
            if(type != "event" && type != "identify") throw new ArgumentException("Type must be either 'event' or 'identify'");
             payload = payloadService.PopulateFromPayload( payload, eventData);
            
            var jsonPayload = new { type, payload };

        

            var response = await client.PostAsJsonAsync("api/send", jsonPayload, options);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase} , {Content}", response.StatusCode, response.ReasonPhrase, content);
            }
            else if(logger.IsEnabled(LogLevel.Information))
            {
              
                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}", response.StatusCode, response.ReasonPhrase, content);
            }

            return response;
        }

        public async Task<HttpResponseMessage> TrackPageView(
            string? url = "", 
            string? title = "", 
            UmamiPayload? payload = null,
            UmamiEventData? eventData = null)
        {
            var sendPayload = payloadService.PopulateFromPayload(payload, eventData);
            if(!string.IsNullOrEmpty(url))
              sendPayload.Url = url;
            if(!string.IsNullOrEmpty(title))
                sendPayload.Title = title;
            return await Send(sendPayload);
        }


        public async Task<HttpResponseMessage> Track(
            string eventName, 
            UmamiEventData? eventData = null)
        {
            var thisPayload = new UmamiPayload
            {
                Name = eventName,
                Data = eventData ?? new UmamiEventData()
            };
            var payload = payloadService.PopulateFromPayload(thisPayload , eventData);
            return await Send(payload);
        }

        public async Task<HttpResponseMessage> Track(UmamiPayload eventObj,
            UmamiEventData? eventData = null)
        {
            var payload = eventObj;
            payload.Data = eventData ?? new UmamiEventData();
            payload.Website = settings.WebsiteId;
            return await Send(payload);
        }

        public async Task<HttpResponseMessage> Identify(UmamiEventData eventData)
        {
            var payload = new UmamiPayload
            {
                Website = settings.WebsiteId,
                Data = eventData,
               
            };

            return await Send(payload, null, "identify");
        }
    }
}