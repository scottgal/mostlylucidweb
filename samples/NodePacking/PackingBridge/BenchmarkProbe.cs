using System.Diagnostics;
using System.Text.Json;
using Microsoft.JavaScript.NodeApi;

namespace PackingTutorial;

[JSExport]
public static class BenchmarkProbe
{
    public static string Echo(string payload) => payload;
    public static EchoValue EchoTyped(EchoValue payload) => payload;

    public static string Memory() => JsonSerializer.Serialize(new
    {
        workingSetBytes = Process.GetCurrentProcess().WorkingSet64,
        managedHeapBytes = GC.GetTotalMemory(forceFullCollection: false)
    });
}
