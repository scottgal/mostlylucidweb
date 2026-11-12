using Mostlylucid.Packing.Planning;
using Mostlylucid.Relevance;
using static AdaptiveAllocation.AllocationModel;

namespace AdaptiveAllocation;

public enum ExecutionMode { Local, Online, Deferred }

public sealed record WorkRequest(string Id, string Task, string ContextVersion, string? CurrentSupplier,
    bool Interactive = true, bool AllowRemote = true, int MaxLatencyMs = 5_000, long ValuePoints = 100,
    long ChangePenalty = 0);

public sealed record ModelSupplier(string Id, ExecutionMode Mode, string TokenizerVersion,
    IReadOnlySet<string> Tasks, int MaxContextTokens, long PrefillTokens, long OutputTokens, int Slots,
    int InputPointsPerThousand, int OutputPointsPerThousand, int BaseLatencyMs, int ReplayMsPerThousand,
    float[] PreferenceTraits, int CachedInputPointsPerThousand = 0);

// These are host-provided runtime facts and target-tokenizer estimates, not claims made by an LLM.
public sealed record ContextLease(string WorkId, string ContextVersion, string TokenizerVersion,
    int RetainedTokens, DateTimeOffset ExpiresAt);
public sealed record SupplierSnapshot(ModelSupplier Supplier, IReadOnlyList<ContextLease> Contexts);
public sealed record TokenQuote(string WorkId, string SupplierId, string TokenizerVersion,
    int NewInputTokens, int HistoryTokens, int OutputTokens);

public sealed record RoutingChoice(AllocationOption Allocation, string Supplier, long PrefillTokens,
    long ReplayTokens, long ReusedContextTokens, long FullContextTokens, long EstimatedLatencyMs, OutcomeEstimate Evidence);
public sealed record RoutingRejection(string Work, string Supplier, string Reason);
public sealed record RoutingInput(PackingProblem Problem, IReadOnlyDictionary<string, RoutingChoice> Choices,
    IReadOnlyList<RoutingRejection> Rejected);

public static class WorkRouting
{
    public static async Task<RoutingInput> BuildAsync(IReadOnlyList<WorkRequest> work,
        IReadOnlyList<SupplierSnapshot> suppliers, IReadOnlyList<TokenQuote> quotes, OutcomeMemory outcomes,
        DateTimeOffset asOf, long revision = 0, RelevanceEngine? preferences = null,
        CancellationToken cancellationToken = default)
    {
        var requests = work.ToDictionary(request => request.Id, StringComparer.Ordinal);
        var providers = suppliers.ToDictionary(snapshot => snapshot.Supplier.Id, StringComparer.Ordinal);
        var estimates = quotes.ToDictionary(quote => (quote.WorkId, quote.SupplierId));
        foreach (var quote in quotes)
        {
            if (!requests.ContainsKey(quote.WorkId) || !providers.ContainsKey(quote.SupplierId))
                throw new ArgumentException("A token quote references unknown work or a supplier.");
            if (quote.NewInputTokens <= 0 || quote.HistoryTokens < 0 || quote.OutputTokens < 0)
                throw new ArgumentException("Token estimates must be nonnegative with positive new input.");
        }

        var capacities = new List<Capacity>();
        foreach (var snapshot in suppliers)
        {
            var supplier = snapshot.Supplier;
            ArgumentException.ThrowIfNullOrWhiteSpace(supplier.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(supplier.TokenizerVersion);
            if (!Enum.IsDefined(supplier.Mode) || supplier.MaxContextTokens <= 0 || supplier.PrefillTokens < 0 ||
                supplier.OutputTokens < 0 || supplier.Slots < 0 || supplier.InputPointsPerThousand < 0 ||
                supplier.OutputPointsPerThousand < 0 || supplier.CachedInputPointsPerThousand < 0 ||
                supplier.BaseLatencyMs < 0 || supplier.ReplayMsPerThousand < 0)
                throw new ArgumentException("Invalid supplier capacity or cost.");
            if (snapshot.Contexts.Any(context => context.RetainedTokens < 0))
                throw new ArgumentException("Retained context cannot be negative.");
            capacities.Add(new(Dimension(supplier.Id + "/prefill"), supplier.PrefillTokens));
            capacities.Add(new(Dimension(supplier.Id + "/output"), supplier.OutputTokens));
            capacities.Add(new(Dimension(supplier.Id + "/slots"), supplier.Slots));
        }

        var choices = new List<RoutingChoice>();
        var rejected = new List<RoutingRejection>();
        foreach (var request in work)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Task);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.ContextVersion);
            if (request.MaxLatencyMs < 0 || request.ValuePoints < 0 || request.ChangePenalty < 0)
                throw new ArgumentException("Invalid work policy.");
            var alternatives = new List<RoutingChoice>();
            foreach (var snapshot in suppliers)
            {
                var supplier = snapshot.Supplier;
                string? reason = !supplier.Tasks.Contains(request.Task) ? "UnsupportedTask"
                    : !request.AllowRemote && supplier.Mode != ExecutionMode.Local ? "RemoteNotAllowed"
                    : request.Interactive && supplier.Mode == ExecutionMode.Deferred ? "InteractiveRequired"
                    : null;
                if (reason is not null) { Reject(reason); continue; }
                if (!estimates.TryGetValue((request.Id, supplier.Id), out var quote)) { Reject("MissingTokenQuote"); continue; }
                if (quote.TokenizerVersion != supplier.TokenizerVersion) { Reject("TokenizerChanged"); continue; }
                var evidence = outcomes.Estimate(request.Task, supplier.Id);
                if (evidence.SmoothedSuccess is null) { Reject("InsufficientOutcomeEvidence"); continue; }

                var retained = snapshot.Contexts.SingleOrDefault(context => context.WorkId == request.Id &&
                    context.ContextVersion == request.ContextVersion && context.TokenizerVersion == supplier.TokenizerVersion &&
                    context.ExpiresAt > asOf)?.RetainedTokens ?? 0;
                if (retained > quote.HistoryTokens)
                    throw new ArgumentException("The context lease exceeds the quoted history.");
                var replay = (long)quote.HistoryTokens - retained;
                var prefill = quote.NewInputTokens + replay;
                // Reusing a prefix reduces prefill work, never the full context-window requirement.
                var fullContext = (long)quote.NewInputTokens + quote.HistoryTokens + quote.OutputTokens;
                var latency = checked(supplier.BaseLatencyMs + Charge(replay, supplier.ReplayMsPerThousand));
                if (fullContext > supplier.MaxContextTokens) { Reject("ContextWindow"); continue; }
                if (prefill > supplier.PrefillTokens || quote.OutputTokens > supplier.OutputTokens || supplier.Slots == 0)
                { Reject("Capacity"); continue; }
                if (latency > request.MaxLatencyMs) { Reject("EstimatedLatency"); continue; }

                var demands = new List<Consumption> { new(Dimension(supplier.Id + "/slots"), 1) };
                if (prefill > 0) demands.Add(new(Dimension(supplier.Id + "/prefill"), prefill));
                if (quote.OutputTokens > 0) demands.Add(new(Dimension(supplier.Id + "/output"), quote.OutputTokens));
                var cost = checked(Charge(prefill, supplier.InputPointsPerThousand) +
                    Charge(retained, supplier.CachedInputPointsPerThousand) +
                    Charge(quote.OutputTokens, supplier.OutputPointsPerThousand));
                var changeCost = request.CurrentSupplier is not null && request.CurrentSupplier != supplier.Id
                    ? request.ChangePenalty : 0;
                var value = checked((long)Math.Round(request.ValuePoints * evidence.SmoothedSuccess.Value));
                alternatives.Add(new(new AllocationOption(Id(request.Id, supplier.Id), request.Id, demands,
                    value, cost, changeCost), supplier.Id, prefill, replay, retained, fullContext, latency, evidence));

                void Reject(string failure) => rejected.Add(new(request.Id, supplier.Id, failure));
            }

            if (preferences is not null && alternatives.Count > 0)
            {
                var pool = alternatives.Select(choice => new RelevanceItem(choice.Allocation.Id,
                    providers[choice.Supplier].Supplier.PreferenceTraits)).ToArray();
                var ranked = await preferences.RankCandidatesAsync("operator", "next", pool,
                    count: pool.Length, cancellationToken: cancellationToken);
                var admitted = ranked.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
                foreach (var choice in alternatives.Where(choice => !admitted.Contains(choice.Allocation.Id)))
                    rejected.Add(new(request.Id, choice.Supplier, "RelevanceEligibility"));
                var preference = ranked.Select((item, index) => (item.Id, Bonus: item.Reasons.Count > 0
                    ? (long)Math.Ceiling(2.0 * (ranked.Count - index) / ranked.Count) : 0))
                    .ToDictionary(pair => pair.Id, pair => pair.Bonus);
                alternatives = alternatives.Where(choice => admitted.Contains(choice.Allocation.Id)).Select(choice =>
                    choice with { Allocation = choice.Allocation with { Preference = preference[choice.Allocation.Id] } }).ToList();
            }
            choices.AddRange(alternatives);
        }

        var problem = Build(capacities, work.Select(request => new Request(request.Id)),
            choices.Select(choice => choice.Allocation), revision);
        return new(problem, choices.ToDictionary(choice => choice.Allocation.Id, StringComparer.Ordinal), rejected.AsReadOnly());
    }

    // Length-prefixing avoids collisions when application IDs contain separators.
    public static string Id(string request, string supplier) => $"{request.Length}:{request}{supplier.Length}:{supplier}";

    private static long Charge(long units, int perThousand) => checked((checked(units * perThousand) + 999) / 1000);
}
