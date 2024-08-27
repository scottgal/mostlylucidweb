namespace Umami.Net.Models;

using System.Collections.Generic;

public class UmamiPayload
{
    public  string Website { get; set; }= null!;
    public string? Hostname { get; set; }
    public string? Language { get; set; }
    public string? Referrer { get; set; }
    public string? Screen { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Name { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }


    public UmamiEventData? Data { get; set; }
}

public class UmamiEventData : Dictionary<string, object>
{
}