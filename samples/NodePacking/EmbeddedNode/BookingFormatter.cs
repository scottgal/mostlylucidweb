using Microsoft.JavaScript.NodeApi;
using AppInterop;

// This adapter constructs JS values directly for a C#-owned runtime, without text serialization.
internal sealed class BookingFormatter(JSValue module)
{
    public string Format(AppSuggestion offer)
        => (string)module.CallMethod("format", ToJS(offer));

    public async Task<string> FormatAsync(AppSuggestion offer)
    {
        var promise = (JSPromise)module.CallMethod("formatAsync", ToJS(offer));
        return await promise.AsTask<string>(value => (string)value);
    }

    private static JSObject ToJS(AppSuggestion offer)
    {
        var appointments = new JSArray();
        foreach (var appointment in offer.Appointments)
        {
            appointments.Add(new JSObject
            {
                ["clientId"] = appointment.ClientId,
                ["clientName"] = appointment.ClientName,
                ["serviceId"] = appointment.ServiceId,
                ["start"] = appointment.Start,
                ["end"] = appointment.End,
                ["technician"] = appointment.Technician,
                ["technicianName"] = appointment.TechnicianName
            });
        }
        var value = new JSObject
        {
            ["outcome"] = offer.Outcome,
            ["version"] = (double)offer.Version,
            ["candidatesComplete"] = offer.CandidatesComplete,
            ["candidateId"] = offer.CandidateId is { } id ? (JSValue)id : JSValue.Null,
            ["start"] = offer.Start is { } start ? (JSValue)start : JSValue.Null,
            ["appointments"] = appointments
        };
        return value;
    }
}
