namespace StreamingAvailability;

internal static class StreamingChecks
{
    private static int _count;
    private static readonly DateTimeOffset Start = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    public static async Task RunAsync()
    {
        var (clock, ledger, view) = Setup();
        var bundle = Bundle();
        var id = Guid.NewGuid();
        var hold = ledger.TryHold(id, "alice", bundle, TimeSpan.FromMinutes(5));
        Check(hold.Success, "hold created");
        Check(view.Check(bundle).Availability == Availability.Available, "delayed view is only advisory");
        Check(!ledger.TryHold(Guid.NewGuid(), "bob", bundle, TimeSpan.FromMinutes(5)).Success, "authoritative writer rejects competing request");
        var version = ledger.Version;
        Check(ledger.TryHold(id, "alice", bundle, TimeSpan.FromMinutes(5)) == hold && ledger.Version == version, "retry returns prior result without consuming capacity again");
        Throws(() => ledger.TryHold(id, "bob", bundle, TimeSpan.FromMinutes(5)), "idempotency key cannot change meaning");
        view.CatchUp();
        Check(view.Check(bundle).Availability == Availability.Unavailable, "stream applies hold");
        Check(!ledger.Confirm(Guid.NewGuid(), "bob", id).Success, "owner check");
        clock.Advance(TimeSpan.FromMinutes(5));
        Check(!ledger.Confirm(Guid.NewGuid(), "alice", id).Success, "deadline equality is expired even before timer delivery");
        view.CatchUp();
        Check(ledger.GetReservation(id)!.State == ReservationState.Expired && view.Check(bundle).Availability == Availability.Available, "expiry releases all resources");
        Check(ledger.TryHold(id, "alice", bundle, TimeSpan.FromMinutes(5)).ExpiresAt == hold.ExpiresAt &&
            ledger.GetReservation(id)!.State == ReservationState.Expired, "old retry never renews a hold");

        var confirmed = ledger.TryHold(Guid.NewGuid(), "bob", bundle, TimeSpan.FromMinutes(1));
        var confirmationKey = Guid.NewGuid();
        var confirmation = ledger.Confirm(confirmationKey, "bob", confirmed.ReservationId!.Value);
        Check(confirmation.Success && ledger.Confirm(confirmationKey, "bob", confirmed.ReservationId.Value) == confirmation, "confirmation is idempotent");
        clock.Advance(TimeSpan.FromMinutes(2));
        ledger.Tick();
        view.CatchUp();
        Check(ledger.GetReservation(confirmed.ReservationId.Value)!.State == ReservationState.Confirmed, "stale expiry cannot release a confirmed reservation");
        var benchOnly = new[] { bundle[1] };
        Check(view.Check(benchOnly).Availability == Availability.Available, "confirmation did not double count the occupied bench seat");
        var cancellationKey = Guid.NewGuid();
        var cancelled = ledger.Cancel(cancellationKey, "bob", confirmed.ReservationId.Value);
        Check(cancelled.Success && ledger.Cancel(cancellationKey, "bob", confirmed.ReservationId.Value) == cancelled, "cancellation retry is idempotent");
        Check(!ledger.Cancel(Guid.NewGuid(), "bob", confirmed.ReservationId.Value).Success, "second cancellation cannot release twice");
        view.CatchUp();
        Check(view.Check(bundle).Availability == Availability.Available, "cancelled bundle fully released");

        var tiny = new[] { bundle[0] with { Units = 1 } };
        var duplicate = ledger.TryHold(Guid.NewGuid(), "alice", new[] { tiny[0], tiny[0] }, TimeSpan.FromMinutes(1));
        Check(!duplicate.Success, "overlapping demands within a bundle are combined");
        Check(ledger.TryHold(Guid.NewGuid(), "alice", bundle, TimeSpan.FromMinutes(1)).Success, "failed bundle did not partially allocate resources");
        var occupiedStart = bundle[0].Start;
        Check(!ledger.SetCapacity(Guid.NewGuid(), "tech:anna", occupiedStart, occupiedStart + 1, 0).Success,
            "roster edit cannot silently remove committed capacity");
        clock.Advance(TimeSpan.FromMinutes(1));
        view.Advance();
        Check(view.Check(bundle).Availability == Availability.Available, "quiet-period heartbeat expires hold");
        Check(ledger.SetCapacity(Guid.NewGuid(), "tech:anna", occupiedStart, occupiedStart + 1, 0).Success, "unused capacity can close");
        view.CatchUp();
        Check(view.Check(tiny).Availability == Availability.Unavailable, "capacity change reaches projection");
        Throws(() => Slots.Exact(Start.AddSeconds(1)), "off-grid appointment boundary rejected");

        await ContentionAsync();
        Rollover();
        Replay();
        RandomScenarios();
        await WorkerAsync();
        Console.WriteLine($"Passed {_count} streaming availability checks, including 500 generated update scenarios.");
    }

    private static async Task ContentionAsync()
    {
        var (_, ledger, view) = Setup();
        var replies = await Task.WhenAll(Enumerable.Range(0, 32).Select(i => Task.Run(() =>
            ledger.TryHold(Guid.NewGuid(), "customer-" + i, Bundle(), TimeSpan.FromMinutes(5)))));
        Check(replies.Count(reply => reply.Success) == 1, "32 competing commands produce one winner for technician capacity one");
        view.CatchUp();
        Check(view.Check(new[] { Bundle()[1] }).Availability == Availability.Available, "failed contenders never consume another bench seat");
    }

    private static void Rollover()
    {
        var (clock, ledger, _) = Setup();
        var view = new RollingAvailability(ledger, TimeSpan.FromHours(1));
        var retained = new[] { Demand.Between("tech:anna", Start.AddMinutes(30), Start.AddMinutes(40)) };
        Confirm(ledger, retained);
        var entering = new[] { Demand.Between("tech:anna", Start.AddMinutes(65), Start.AddMinutes(70)) };
        Confirm(ledger, entering);
        Check(view.Check(entering).Availability == Availability.OutsideWindow, "outside horizon is unknown to projection");
        clock.Advance(TimeSpan.FromMinutes(10));
        view.Advance();
        Check(view.Check(retained).Availability == Availability.Unavailable, "rollover applies backlog to retained cells");
        Check(view.Check(entering).Availability == Availability.Unavailable, "new tail includes reservations made beyond old horizon");
        Check(view.Cursor == ledger.Version, "tail snapshot and retained events share one version");
        Check(view.Check(new[] { Demand.Between("tech:anna", Start, Start.AddMinutes(5)) }).Availability == Availability.OutsideWindow, "old head evicted");

        var writes = view.CellsWritten;
        clock.Advance(Slots.Size);
        view.Advance();
        Check(view.CellsWritten - writes == 2, "one-slot advance writes only one tail cell per existing resource");
        clock.Advance(TimeSpan.FromDays(2));
        view.Advance();
        var future = new[] { Demand.Between("tech:anna", clock.GetUtcNow().AddMinutes(5), clock.GetUtcNow().AddMinutes(10)) };
        Check(view.Check(future).Availability == Availability.Available, "large jump rebuilds the full new window without leaking old values");

        var (crossClock, crossLedger, _) = Setup();
        var crossView = new RollingAvailability(crossLedger, TimeSpan.FromHours(1));
        var spanning = new[] { Demand.Between("station:bench", Start.AddMinutes(55), Start.AddMinutes(70)) };
        Confirm(crossLedger, spanning);
        crossClock.Advance(TimeSpan.FromMinutes(15));
        crossView.Advance();
        Check(crossView.Check(spanning).Availability == Availability.Available, "overlapping snapshot and retained cells are not double counted");
        Check(crossView.Check(new[] { spanning[0] with { Units = 2 } }).Availability == Availability.Unavailable, "tail retains the one existing user");
    }

    private static void Replay()
    {
        var (_, ledger, view) = Setup();
        Confirm(ledger, Bundle());
        var events = ledger.ReadAfter(view.Cursor);
        Throws(() => view.Apply(events[^1]), "event gap rejected before cursor advances");
        Check(view.Cursor < events[^1].Sequence, "gap cannot skip missing state");
        view.CatchUp();
        var writes = view.CellsWritten;
        foreach (var change in events) view.Apply(change);
        Check(view.CellsWritten == writes, "duplicate delivery is a no-op");
        var recovered = new RollingAvailability(ledger, TimeSpan.FromHours(3));
        Check(recovered.Cursor == view.Cursor && recovered.Check(Bundle()) == view.Check(Bundle()), "snapshot rebuild agrees with streamed state");
    }

    private static void RandomScenarios()
    {
        var (clock, ledger, _) = Setup();
        var view = new RollingAvailability(ledger, TimeSpan.FromHours(2));
        var random = new Random(91);
        var ids = new List<Guid>();
        for (var attempt = 0; attempt < 500; attempt++)
        {
            var start = Slots.Floor(clock.GetUtcNow()) + random.Next(1, 28);
            var end = start + random.Next(1, 7);
            var id = Guid.NewGuid();
            var resource = random.Next(2) == 0 ? "tech:anna" : "station:bench";
            var reply = ledger.TryHold(id, "owner", new[] { new Demand(resource, start, end) }, TimeSpan.FromMinutes(random.Next(1, 6)));
            if (reply.Success) ids.Add(id);
            if (ids.Count > 0)
            {
                var selected = ids[random.Next(ids.Count)];
                if (random.Next(2) == 0) ledger.Confirm(Guid.NewGuid(), "owner", selected);
                else ledger.Cancel(Guid.NewGuid(), "owner", selected);
            }
            clock.Advance(TimeSpan.FromMinutes(random.Next(3)));
            view.Advance();
            var active = ids.Select(id => ledger.GetReservation(id)!)
                .Where(reservation => reservation.State is ReservationState.Held or ReservationState.Confirmed).ToArray();
            foreach (var candidateResource in new[] { "tech:anna", "station:bench" })
            {
                var queryStart = Slots.Floor(clock.GetUtcNow()) + random.Next(1, 18);
                var queryEnd = queryStart + random.Next(1, 6);
                var capacity = candidateResource == "tech:anna" ? 1 : 2;
                var expected = true;
                for (var slot = queryStart; slot < queryEnd; slot++)
                {
                    var used = active.SelectMany(reservation => reservation.Demands)
                        .Where(demand => demand.ResourceId == candidateResource && demand.Start <= slot && slot < demand.End)
                        .Sum(demand => demand.Units);
                    if (used + 1 > capacity) expected = false;
                }
                var answer = view.Check(new[] { new Demand(candidateResource, queryStart, queryEnd) });
                Check((answer.Availability == Availability.Available) == expected, "streamed view agrees with independent reservation-record recount");
            }
            Check(view.Cursor == ledger.Version, "cursor catches up after each generated change");
        }
    }

    private static async Task WorkerAsync()
    {
        var (clock, ledger, view) = Setup();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var worker = AvailabilityWorker.RunAsync(ledger, view, stop.Token);
        try
        {
            var held = ledger.TryHold(Guid.NewGuid(), "worker-test", Bundle(), TimeSpan.FromMinutes(1));
            Check(held.Success, "worker test hold");
            await UntilAsync(() => view.Cursor == ledger.Version);
            Check(view.Check(Bundle()).Availability == Availability.Unavailable, "channel wake-up drains journal incrementally");
            clock.Advance(TimeSpan.FromMinutes(1));
            ledger.Tick();
            await UntilAsync(() => view.Cursor == ledger.Version);
            Check(view.Check(Bundle()).Availability == Availability.Available, "expiry event reaches async worker");
        }
        finally
        {
            await stop.CancelAsync();
            try { await worker; } catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("Worker failed to catch up.");
            await Task.Delay(10);
        }
    }

    private static (ManualClock Clock, BookingLedger Ledger, RollingAvailability View) Setup()
    {
        var clock = new ManualClock(Start);
        var ledger = new BookingLedger(clock);
        var start = Slots.Exact(Start);
        Check(ledger.SetCapacity(Guid.NewGuid(), "tech:anna", start, start + 4 * 288, 1).Success, "fixture technician capacity");
        Check(ledger.SetCapacity(Guid.NewGuid(), "station:bench", start, start + 4 * 288, 2).Success, "fixture station capacity");
        return (clock, ledger, new RollingAvailability(ledger, TimeSpan.FromHours(3)));
    }

    private static Demand[] Bundle() => new[]
    {
        Demand.Between("tech:anna", Start.AddHours(1), Start.AddHours(2).AddMinutes(5)),
        Demand.Between("station:bench", Start.AddHours(1), Start.AddHours(2).AddMinutes(10))
    };

    private static void Confirm(BookingLedger ledger, Demand[] demands)
    {
        var id = Guid.NewGuid();
        Check(ledger.TryHold(id, "owner", demands, TimeSpan.FromMinutes(5)).Success, "fixture hold");
        Check(ledger.Confirm(Guid.NewGuid(), "owner", id).Success, "fixture confirmation");
    }

    private static void Check(bool condition, string name)
    {
        _count++;
        if (!condition) throw new InvalidOperationException("Failed: " + name);
    }

    private static void Throws(Action action, string name)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { Check(true, name); return; }
        throw new InvalidOperationException("Expected failure: " + name);
    }
}
