namespace Mostlylucid.Relevance.PredictDemo;

/// <summary>One independently held-out first-cart forecast, retained before looking at its future.</summary>
public sealed record ScoredJourney(string Subject, string State, string Actual, IReadOnlyDictionary<string, double> Probabilities);
/// <summary>Purchase reliability within one predicted-probability bin. Small bins are noisy, not confidence intervals.</summary>
public sealed record ReliabilityBin(string Range, int Count, double MeanPredicted, double ObservedFrequency);
/// <summary>Simulator-only evaluation, including the population and unresolved/missing-feedback accounting.</summary>
public sealed record PredictionEvaluation(int Visits, int WithoutCart, int Censored, int Scored, int HorizonEvents,
    double ModelBrier, double PriorBrier, IReadOnlyList<ReliabilityBin> PurchaseBins);

/// <summary>Scores held-out journeys at their first cart addition against a frozen population model.</summary>
public static class OutcomeEvaluation
{
    public const string Unresolved = "unresolved";
    public static readonly string[] Classes = ["purchase", "cancel", "leave", Unresolved];

    /// <summary>Gets a target only when the outcome is observed or N future transitions are available.</summary>
    public static string? Target(FixtureJourney journey, int cartIndex, int horizon)
    {
        var next = journey.Events.Skip(cartIndex + 1).Take(horizon).ToArray();
        var outcome = next.FirstOrDefault(e => Classes.Take(3).Contains(e.Type));
        return outcome?.Type ?? (next.Length == horizon ? Unresolved : null);
    }

    /// <summary>Multiclass Brier score: sum squared class errors, averaged over episodes elsewhere; range zero to two.</summary>
    public static double Brier(IReadOnlyDictionary<string, double> forecast, string actual) =>
        Classes.Sum(destination => Math.Pow(forecast.GetValueOrDefault(destination) - (destination == actual ? 1 : 0), 2));

    public static Dictionary<string, double> Distribution(DestinationPrediction prediction) =>
        prediction.Outcomes.ToDictionary(p => p.Destination, p => p.Probability)
            .Append(new KeyValuePair<string, double>(Unresolved, prediction.RemainingProbability)).ToDictionary();

    /// <summary>A training-only landmark prior; missing feedback is excluded rather than relabelled as failure.</summary>
    public static Dictionary<string, double> Prior(IReadOnlyList<FixtureJourney> training, int horizon)
    {
        var counts = Classes.ToDictionary(c => c, _ => 0.0);
        foreach (var journey in training)
        {
            var cart = FirstCart(journey);
            if (cart >= 0 && Target(journey, cart, horizon) is { } actual) counts[actual]++;
        }
        var total = counts.Values.Sum();
        return counts.ToDictionary(p => p.Key, p => total > 0 ? p.Value / total : 1.0 / Classes.Length);
    }

    /// <summary>Forecasts all prefixes before examining their labels; no held-out outcome enters the evaluator engine.</summary>
    public static async Task<(PredictionEvaluation Summary, IReadOnlyList<ScoredJourney> Rows)> RunAsync(
        RelevanceEngine evaluator, PredictionFixture fixture, int horizon, CancellationToken cancellationToken = default)
    {
        var pending = new List<(FixtureJourney Journey, int Cart, DestinationPrediction Forecast)>();
        var withoutCart = 0;
        foreach (var journey in fixture.Holdout)
        {
            var cart = FirstCart(journey);
            if (cart < 0) { withoutCart++; continue; }
            var encoder = new JourneyState();
            foreach (var observation in journey.Events.Take(cart + 1))
                await PredictionDemo.SaveAsync(evaluator, observation, encoder, cancellationToken);
            pending.Add((journey, cart, await evaluator.PredictAsync(journey.User.Id, cancellationToken)));
        }
        // Reveal future labels only after every forecast has been captured.
        var rows = new List<ScoredJourney>();
        foreach (var (journey, cart, forecast) in pending)
            if (Target(journey, cart, horizon) is { } actual)
                rows.Add(new(journey.User.Id, forecast.CurrentState!, actual, Distribution(forecast)));
        if (rows.Count == 0) throw new InvalidOperationException("No scorable held-out episodes; generate a larger fixture.");
        var prior = Prior(fixture.Training, horizon);
        var bins = Enumerable.Range(0, 5).Select(bin =>
        {
            var members = rows.Where(r => Math.Min(4, (int)(r.Probabilities["purchase"] * 5)) == bin).ToArray();
            return new ReliabilityBin($"{bin * 20}–{(bin + 1) * 20}%", members.Length,
                members.Length == 0 ? 0 : members.Average(r => r.Probabilities["purchase"]),
                members.Length == 0 ? 0 : members.Average(r => r.Actual == "purchase" ? 1.0 : 0));
        }).ToArray();
        return (new(fixture.Holdout.Count, withoutCart, pending.Count - rows.Count, rows.Count, horizon,
            rows.Average(r => Brier(r.Probabilities, r.Actual)), rows.Average(r => Brier(prior, r.Actual)), bins), rows);
    }
    public static int FirstCart(FixtureJourney journey) => journey.Events.ToList().FindIndex(e => e.Type == "cart");
}
