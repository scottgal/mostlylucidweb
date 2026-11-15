using System.Runtime.InteropServices.JavaScript;

public static partial class BrowserInterop
{
    [JSImport("getName", "calendarApi")]
    private static partial string GetName(string id);

    [JSExport]
    public static string DescribeAppointment(string clientId, string serviceId, string technicianId)
        => $"{GetName(clientId)}: {serviceId} with {GetName(technicianId)}";
}
