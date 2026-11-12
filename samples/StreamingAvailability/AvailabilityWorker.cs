namespace StreamingAvailability;

public static class AvailabilityWorker
{
    // One event consumer; a periodic tick also expires holds and moves the horizon when the venue is quiet.
    public static async Task RunAsync(BookingLedger ledger, RollingAvailability view, CancellationToken cancellationToken)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = stop.Token;
        var events = ConsumeAsync();
        var clock = AdvanceAsync();
        await Task.WhenAny(events, clock);
        await stop.CancelAsync();
        await Task.WhenAll(events, clock);

        async Task ConsumeAsync()
        {
            await foreach (var _ in ledger.Changes.ReadAllAsync(token)) view.CatchUp();
        }

        async Task AdvanceAsync()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), ledger.Clock);
            while (await timer.WaitForNextTickAsync(token)) view.Advance();
        }
    }
}
