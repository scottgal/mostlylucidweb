using Microsoft.JavaScript.NodeApi;

public static class NativeExports
{
    // Small, fully tested packaging proof. The complete scheduler needs a separate AOT audit.
    [JSExport]
    public static string DescribeAppointment(string clientId, string serviceId,
        string technicianId, Func<string, string> getName)
        => $"{getName(clientId)}: {serviceId} with {getName(technicianId)}";
}
