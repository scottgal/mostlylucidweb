using Microsoft.JavaScript.NodeApi;

namespace AppInterop;

[JSExport]
public struct ScheduleWindow
{
    public string Start { get; set; }
    public string End { get; set; }
}

// Columns have equal lengths; row i identifies one resource/grid cell. No JSON is involved.
[JSExport]
public struct AvailabilityInput
{
    public long Version { get; set; }
    public string[] ResourceIds { get; set; }
    public long[] Slots { get; set; }
    public int[] Capacity { get; set; }
    public long[] Used { get; set; }
}

[JSExport]
public sealed class AppAppointment
{
    public string ClientId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ServiceId { get; set; } = "";
    public string Technician { get; set; } = "";
    public string TechnicianName { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
}

[JSExport]
public sealed class DemandView
{
    public string ResourceId { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public int Units { get; set; }
}

[JSExport]
public sealed class AppSuggestion
{
    public string Outcome { get; set; } = "";
    public long Version { get; set; }
    public bool CandidatesComplete { get; set; }
    public string? CandidateId { get; set; }
    public string? Start { get; set; }
    public AppAppointment[] Appointments { get; set; } = [];
    public DemandView[] Demands { get; set; } = [];
}
