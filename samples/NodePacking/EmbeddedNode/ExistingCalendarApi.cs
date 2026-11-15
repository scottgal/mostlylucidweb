using AppInterop;
using Microsoft.JavaScript.NodeApi;

internal sealed class ExistingCalendarApi : IDisposable
{
    private readonly JSReference _api;

    public ExistingCalendarApi(JSValue module, AutoScheduler engine)
    {
        var resources = new JSArray();
        foreach (var id in engine.ResourceIds) resources.Add(id);
        var catalog = new JSObject
        {
            ["start"] = engine.HorizonStart, ["end"] = engine.HorizonEnd,
            ["gridMinutes"] = engine.GridMinutes, ["resourceIds"] = resources
        };
        _api = new JSReference(module.CallMethod("createCalendarApi", catalog));
    }

    public async Task<AvailabilityInput> ReadWindowAsync(ScheduleWindow window)
    {
        var promise = (JSPromise)_api.GetValue().CallMethod("readWindow", window.Start, window.End);
        return await promise.AsTask(ReadSnapshot);
    }

    private static AvailabilityInput ReadSnapshot(JSValue value) => new()
    {
        Version = checked((long)(double)value["version"]),
        ResourceIds = ((JSArray)value["resourceIds"]).Select(x => (string)x).ToArray(),
        Slots = ((JSArray)value["slots"]).Select(x => checked((long)(double)x)).ToArray(),
        Capacity = ((JSArray)value["capacity"]).Select(x => (int)x).ToArray(),
        Used = ((JSArray)value["used"]).Select(x => checked((long)(double)x)).ToArray()
    };

    public string GetName(string id) => (string)_api.GetValue().CallMethod("getName", id);

    public async Task<bool> TryBookAsync(AppSuggestion offer, string commandId)
    {
        var demands = new JSArray();
        foreach (var demand in offer.Demands)
            demands.Add(new JSObject
            {
                ["resourceId"] = demand.ResourceId, ["start"] = demand.Start,
                ["end"] = demand.End, ["units"] = demand.Units
            });
        var promise = (JSPromise)_api.GetValue().CallMethod("tryBook", offer.CandidateId!,
            (double)offer.Version, commandId, demands);
        return await promise.AsTask(value => (bool)value["success"]);
    }

    public void Dispose() => _api.Dispose();
}
