using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.Helpers;
using Umami.Net.Models;

namespace Umami.Net
{
    public class UmamiClient(
        HttpClient client,
        ILogger<UmamiClient> logger,
        UmamiClientSettings settings)
    {
        private readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new LowerCaseNamingPolicy(), // Custom naming policy for lower-cased properties
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
        {
            var newPayload = GetPayload(data: data);
            
            if(payload==null) return newPayload;
            if(payload.Hostname != null)
                newPayload.Hostname = payload.Hostname;
          
            if(payload.Language != null)
                newPayload.Language = payload.Language;
            
            if(payload.Referrer != null)
                newPayload.Referrer = payload.Referrer;
            
            if(payload.Screen != null)
                newPayload.Screen = payload.Screen;
            
            if(payload.Title != null)
                newPayload.Title = payload.Title;
            
            if(payload.Url != null)
                newPayload.Url = payload.Url;
            
            if(payload.Name != null)
                newPayload.Name = payload.Name;
            
            if(payload.Data != null)
                newPayload.Data = payload.Data;
            
            return newPayload;

          
            
        }
        
        private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
        {
            var payload = new UmamiPayload
            {
                Website = settings.WebsiteId,
                Data = data,
                Url = url ?? string.Empty
            };
            

            return payload;
        }

        public async Task<HttpResponseMessage> Send(UmamiPayload? payload=null, UmamiEventData? eventData =null,  string type = "event")
        {
             payload = PopulateFromPayload(payload, eventData);
            
            var jsonPayload = new { type, payload };
            logger.LogInformation("Sending data to Umami: {Payload}", JsonSerializer.Serialize(jsonPayload, options));

            var response = await client.PostAsJsonAsync("api/send", jsonPayload, options);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}", response.StatusCode, response.ReasonPhrase, content);
            }

            return response;
        }

        public async Task<HttpResponseMessage> TrackUrl(string? url = "", string? eventName = "event", UmamiEventData? eventData = null)
        {
            var payload = GetPayload(url);
            payload.Name = eventName;
            return await Track(payload, eventData);
        }

        public async Task<HttpResponseMessage> Track(string eventObj, UmamiEventData? eventData = null)
        {
            var payload = new UmamiPayload
            {
                Website = settings.WebsiteId,
                Name = eventObj,
                Data = eventData ?? new UmamiEventData()
            };

            return await Send(payload);
        }

        public async Task<HttpResponseMessage> Track(UmamiPayload eventObj, UmamiEventData? eventData = null)
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
                Data = eventData
            };

            return await Send(payload, null, "identify");
        }
    }
}