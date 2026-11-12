using StreamingAvailability;

if (args.Contains("--check"))
{
    await StreamingChecks.RunAsync();
    return;
}

var now = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
var clock = new ManualClock(now);
var ledger = new BookingLedger(clock);
for (var day = 0; day < 3; day++)
{
    var start = Slots.Exact(now.AddDays(day));
    var end = Slots.Exact(now.AddDays(day).AddHours(8));
    ledger.SetCapacity(Guid.NewGuid(), "tech:anna", start, end, 1);
    ledger.SetCapacity(Guid.NewGuid(), "station:bench", start, end, 2);
}
var view = new RollingAvailability(ledger, TimeSpan.FromDays(2));
var bundle = new[]
{
    Demand.Between("tech:anna", now.AddHours(1), now.AddHours(2).AddMinutes(5)),
    Demand.Between("station:bench", now.AddHours(1), now.AddHours(2).AddMinutes(10))
};
Console.WriteLine($"Cached view before hold: {view.Check(bundle).Availability}");
var command = Guid.NewGuid();
var hold = ledger.TryHold(command, "alice", bundle, TimeSpan.FromMinutes(5));
Console.WriteLine($"Alice's hold: {hold.Reason}");
Console.WriteLine($"Delayed view still says: {view.Check(bundle).Availability}");
Console.WriteLine($"Bob's competing hold: {ledger.TryHold(Guid.NewGuid(), "bob", bundle, TimeSpan.FromMinutes(5)).Reason}");
view.CatchUp();
Console.WriteLine($"Caught-up view: {view.Check(bundle).Availability}");
clock.Advance(TimeSpan.FromMinutes(5));
Console.WriteLine($"Confirm exactly at expiry: {ledger.Confirm(Guid.NewGuid(), "alice", hold.ReservationId!.Value).Reason}");
view.CatchUp();
Console.WriteLine($"After expiry: {view.Check(bundle).Availability}");
var next = ledger.TryHold(Guid.NewGuid(), "bob", bundle, TimeSpan.FromMinutes(5));
Console.WriteLine($"Bob confirms: {ledger.Confirm(Guid.NewGuid(), "bob", next.ReservationId!.Value).Reason}");
view.CatchUp();
Console.WriteLine($"Confirmed usage remains: {view.Check(bundle).Availability}");

var future = new[] { Demand.Between("tech:anna", now.AddDays(2).AddHours(2), now.AddDays(2).AddHours(3)) };
var futureHold = ledger.TryHold(Guid.NewGuid(), "carol", future, TimeSpan.FromMinutes(5));
ledger.Confirm(Guid.NewGuid(), "carol", futureHold.ReservationId!.Value);
Console.WriteLine($"Day-three query before window moves: {view.Check(future).Availability}");
clock.Advance(TimeSpan.FromDays(1) - TimeSpan.FromMinutes(5));
var before = view.CellsWritten;
view.Advance();
Console.WriteLine($"Day-three query after window moves: {view.Check(future).Availability}");
Console.WriteLine($"Tail cells written: {view.CellsWritten - before}");
