using System.Globalization;
using Microsoft.JavaScript.NodeApi;
using Mostlylucid.Packing;
using Mostlylucid.Packing.Calendar;
using PackingTutorial;

namespace AppInterop;

[JSExport]
public sealed class AutoScheduler
{
    private readonly CalendarDefinition _definition = SalonDefinition.Create();
    public string HorizonStart => _definition.Bounds.From.ToString("O");
    public string HorizonEnd => _definition.Bounds.To.ToString("O");
    public int GridMinutes => (int)_definition.Grid.TotalMinutes;
    public string[] ResourceIds => _definition.Resources.Keys
        .Concat(_definition.Clients.Keys.Select(CalendarDefinition.ClientResource)).ToArray();

    public AppSuggestion Suggest(ScheduleWindow window, AvailabilityInput input, Func<string, string> getName)
    {
        ArgumentNullException.ThrowIfNull(getName);
        var store = new SnapshotStore(_definition, input);
        var result = Pack.Calendar(_definition, store).FindTogether(Parse(window.Start), Parse(window.End),
            [new("gels", "alice"), new("pedicure", "beth")], limit: 1);
        var match = result.Best;
        return new AppSuggestion
        {
            Outcome = result.Outcome.ToString(), Version = result.Version,
            CandidatesComplete = result.CandidatesComplete, CandidateId = match?.Id,
            Start = match?.Start.ToString("O"),
            Appointments = match?.Appointments.Select(appointment => new AppAppointment
            {
                ClientId = appointment.ClientId, ClientName = getName(appointment.ClientId),
                ServiceId = appointment.ServiceId, Technician = appointment.Resources["technician"],
                TechnicianName = getName(appointment.Resources["technician"]),
                Start = appointment.Start.ToString("O"), End = appointment.End.ToString("O")
            }).ToArray() ?? [],
            Demands = match?.Demands.Select(demand => new DemandView
            {
                ResourceId = demand.ResourceId, Start = demand.Time.From.ToString("O"),
                End = demand.Time.To.ToString("O"), Units = demand.Units
            }).ToArray() ?? []
        };
    }

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    // A per-call read-only view of the application's snapshot, never a second reservation database.
    private sealed class SnapshotStore : ICalendarStore
    {
        private readonly CalendarSnapshot _snapshot;
        public TimeProvider Clock => TimeProvider.System;

        public SnapshotStore(CalendarDefinition definition, AvailabilityInput input)
        {
            if (input.ResourceIds is null || input.Slots is null || input.Capacity is null || input.Used is null ||
                input.Slots.Length != input.ResourceIds.Length || input.Capacity.Length != input.Slots.Length ||
                input.Used.Length != input.Slots.Length)
                throw new ArgumentException("Snapshot columns must have equal lengths.");
            var known = definition.Resources.Keys.Concat(definition.Clients.Keys.Select(CalendarDefinition.ClientResource))
                .ToHashSet(StringComparer.Ordinal);
            var capacity = new Dictionary<ResourceSlot, int>();
            var used = new Dictionary<ResourceSlot, long>();
            var first = definition.Slot(definition.Bounds.From);
            var end = definition.Slot(definition.Bounds.To);
            for (var i = 0; i < input.Slots.Length; i++)
            {
                if (!known.Contains(input.ResourceIds[i]) || input.Slots[i] < first || input.Slots[i] >= end ||
                    input.Capacity[i] < 0 || input.Used[i] < 0)
                    throw new ArgumentException("Invalid snapshot cell.");
                var key = new ResourceSlot(input.ResourceIds[i], input.Slots[i]);
                if (!capacity.TryAdd(key, input.Capacity[i])) throw new ArgumentException("Duplicate snapshot cell.");
                used.Add(key, input.Used[i]);
            }
            _snapshot = new(input.Version, definition.Version, capacity, used);
        }

        public CalendarSnapshot Capture(Period window) => _snapshot;
        public ReservationReply TryHold(HoldCommand command) => throw ReadOnly();
        public ReservationReply Confirm(Guid commandId, string owner, Guid holdId) => throw ReadOnly();
        public ReservationReply Cancel(Guid commandId, string owner, Guid reservationId) => throw ReadOnly();
        public BlockPage GetBlocks(Period window, IReadOnlyList<string> resources, int limit = 1000) => throw ReadOnly();
        private static NotSupportedException ReadOnly() => new("Commit through the existing application API.");
    }
}
