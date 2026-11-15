using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.JavaScript.NodeApi;
using Mostlylucid.Packing;
using Mostlylucid.Packing.Calendar;
using Mostlylucid.Packing.Sqlite;

namespace PackingTutorial;

[JSExport]
public sealed class Salon
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SqliteCalendarStore _store;
    private readonly CalendarSpace _calendar;
    private readonly Dictionary<string, CalendarMatch> _offers = new(StringComparer.Ordinal);
    private bool _disposed;

    static Salon()
    {
        // The Node host's dependency context does not describe this library's native assets.
        NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly,
            (name, _, _) =>
            {
                if (name != "e_sqlite3") return IntPtr.Zero;
                var library = OperatingSystem.IsWindows() ? "e_sqlite3.dll" :
                    OperatingSystem.IsMacOS() ? "libe_sqlite3.dylib" : "libe_sqlite3.so";
                var directory = Path.GetDirectoryName(typeof(Salon).Assembly.Location)!;
                var path = Path.Combine(directory, "runtimes", RuntimeInformation.RuntimeIdentifier,
                    "native", library);
                if (!File.Exists(path)) path = Path.Combine(directory, library);
                return NativeLibrary.Load(path);
            });
    }

    public Salon(string databasePath)
    {
        _store = new SqliteCalendarStore(databasePath, SalonDefinition.Create(), new DemoClock());
        _calendar = Pack.Calendar(SalonDefinition.Create(), _store);
    }

    public SearchOffer FindTogether(SearchWindow window)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = _calendar.FindTogether(Parse(window.Start), Parse(window.End),
            [new("gels", "alice"), new("pedicure", "beth")], limit: 1);
        // This tiny CLI retains only the current offer. A server needs a bounded, expiring offer cache.
        _offers.Clear();
        var match = result.Best;
        if (match is not null) _offers.Add(match.Id, match);
        return new SearchOffer
        {
            Outcome = result.Outcome.ToString(), Version = result.Version,
            CandidatesComplete = result.CandidatesComplete,
            CandidateId = match?.Id, Start = match?.Start.ToString("O"),
            Appointments = match?.Appointments.Select(appointment => new AppointmentView
            {
                ClientId = appointment.ClientId, ServiceId = appointment.ServiceId,
                Start = appointment.Start.ToString("O"), End = appointment.End.ToString("O"),
                Technician = appointment.Resources["technician"]
            }).ToArray() ?? []
        };
    }

    // Retained only for the JSON baseline and the gRPC adapter comparison.
    public string FindTogetherJson(SearchWindow window) => Encode(FindTogether(window));

    public ReservationResult TryHold(string candidateId, string commandId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_offers.TryGetValue(candidateId, out var match))
            throw new ArgumentException("Search for an offer in this session first.", nameof(candidateId));
        return Project(_calendar.TryHold(Guid.Parse(commandId), "alice", match, TimeSpan.FromMinutes(5)));
    }

    public ReservationResult Confirm(string reservationId, string commandId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Project(_calendar.Confirm(Guid.Parse(commandId), "alice", Guid.Parse(reservationId)));
    }

    public bool CanSarahConsult(string at)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _calendar.Fits(Parse(at), TimeSpan.FromMinutes(15),
            new BookingRequirements("consult", "other",
                RequiredResources: new Dictionary<string, string> { ["technician"] = "sarah" })).Fits;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _offers.Clear();
        _store.Dispose();
    }

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    private static string Encode<T>(T value) => JsonSerializer.Serialize(value, Json);

    private static ReservationResult Project(ReservationReply reply) => new()
    {
        Success = reply.Success, Reason = reply.Reason,
        ReservationId = reply.ReservationId?.ToString(), Version = reply.Version,
        ExpiresAt = reply.ExpiresAt?.ToString("O")
    };

    // A fixed clock makes this future-dated tutorial reproducible, including hold deadlines.
    private sealed class DemoClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => SalonDefinition.At(9);
    }
}
