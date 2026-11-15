using System.Runtime.InteropServices;
using Microsoft.JavaScript.NodeApi;
using Microsoft.JavaScript.NodeApi.Runtime;
using AppInterop;

var library = OperatingSystem.IsWindows() ? "libnode.dll" :
    OperatingSystem.IsMacOS() ? "libnode.dylib" : "libnode.so";
var nativePath = Path.Combine(AppContext.BaseDirectory,
    "runtimes", RuntimeInformation.RuntimeIdentifier, "native", library);
if (!File.Exists(nativePath)) nativePath = Path.Combine(AppContext.BaseDirectory, library);

using var platform = new NodeEmbeddingPlatform(new NodeEmbeddingPlatformSettings
{
    LibNodePath = nativePath
});
using var runtime = platform.CreateThreadRuntime(AppContext.BaseDirectory,
    new NodeEmbeddingRuntimeSettings
    {
        MainScript = "globalThis.require = require('module').createRequire(process.execPath);"
    });

var engine = new AutoScheduler();
var window = new ScheduleWindow
{
    Start = "2030-06-03T10:00:00Z", End = "2030-06-03T12:00:00Z"
};
var api = runtime.Run(() => new ExistingCalendarApi(
    runtime.Import(Path.Combine(AppContext.BaseDirectory, "existing-calendar-api.cjs")), engine));
try
{
    var snapshot = await runtime.RunAsync(() => api.ReadWindowAsync(window));
    var offer = runtime.Run(() => engine.Suggest(window, snapshot, api.GetName));
    if (offer.CandidateId is null) throw new InvalidOperationException("No appointment found.");
    var modulePath = Path.Combine(AppContext.BaseDirectory, "format-booking.cjs");
    var text = runtime.Run(() =>
        new BookingFormatter(runtime.Import(modulePath)).Format(offer));
    Console.WriteLine(text);
    if (!text.Contains("11:00 London") || !text.Contains("Alice: gels with Sarah"))
        throw new InvalidOperationException("Unexpected booking summary.");
    Console.WriteLine("PASS: C# called JavaScript for availability and names.");
    var booked = await runtime.RunAsync(() => api.TryBookAsync(offer, Guid.NewGuid().ToString()));
    if (!booked) throw new InvalidOperationException("The application API rejected the booking.");
    Console.WriteLine("PASS: Existing JavaScript API committed the booking.");

    var asyncText = await runtime.RunAsync(() =>
        new BookingFormatter(runtime.Import(modulePath)).FormatAsync(offer));
    if (asyncText != text) throw new InvalidOperationException("Promise result differs from sync result.");
    Console.WriteLine("PASS: JavaScript Promise awaited as a C# Task.");

    if (args.Contains("--verify-errors"))
    {
        var caughtSync = false;
        try
        {
            runtime.Run(() => runtime.Import(modulePath).CallMethod("format", JSValue.Null));
        }
        catch (JSException)
        {
            caughtSync = true;
            Console.WriteLine("PASS: JavaScript throw arrived as JSException in C#.");
        }
        if (!caughtSync) throw new InvalidOperationException("Expected JavaScript error.");

        var caughtAsync = false;
        try
        {
            await runtime.RunAsync(async () =>
            {
                var promise = (JSPromise)runtime.Import(modulePath).CallMethod("formatAsync", JSValue.Null);
                return await promise.AsTask<string>(value => (string)value);
            });
        }
        catch (JSException)
        {
            caughtAsync = true;
            Console.WriteLine("PASS: JavaScript Promise rejection arrived as JSException in C#.");
        }
        if (!caughtAsync) throw new InvalidOperationException("Expected rejected JavaScript Promise.");
        var recovered = runtime.Run(() => new BookingFormatter(runtime.Import(modulePath)).Format(offer));
        if (recovered != text) throw new InvalidOperationException("Runtime did not recover after errors.");
    }
}
finally
{
    runtime.Run(api.Dispose);
}
