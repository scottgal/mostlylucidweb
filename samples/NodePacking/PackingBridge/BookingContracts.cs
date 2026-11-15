using Microsoft.JavaScript.NodeApi;

namespace PackingTutorial;

// Small request/reply structs cross by value. Nested result classes use managed object wrappers.
[JSExport]
public struct SearchWindow
{
    public string Start { get; set; }
    public string End { get; set; }
}

[JSExport]
public sealed class AppointmentView
{
    public string ClientId { get; set; } = "";
    public string ServiceId { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public string Technician { get; set; } = "";
}

[JSExport]
public sealed class SearchOffer
{
    public string Outcome { get; set; } = "";
    public long Version { get; set; }
    public bool CandidatesComplete { get; set; }
    public string? CandidateId { get; set; }
    public string? Start { get; set; }
    public AppointmentView[] Appointments { get; set; } = [];
}

[JSExport]
public struct ReservationResult
{
    public bool Success { get; set; }
    public string Reason { get; set; }
    public string? ReservationId { get; set; }
    public long Version { get; set; }
    public string? ExpiresAt { get; set; }
}

[JSExport]
public struct EchoValue
{
    public string Message { get; set; }
}
