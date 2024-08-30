using Umami.Net.Models;

namespace Umami.Net.Test.MessageHandlers;

public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}