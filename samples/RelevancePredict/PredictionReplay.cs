namespace Mostlylucid.Relevance.PredictDemo;

/// <summary>One visible prefix and its actual engine forecast. No future label is supplied to the predictor.</summary>
public sealed record ReplayFrame(string? Event, DestinationPrediction Prediction);
/// <summary>A labelled synthetic visit for the article; names describe the observed path, not hidden intent.</summary>
public sealed record ReplayParticipant(string Id, string Name, IReadOnlyList<ReplayFrame> Frames);
/// <summary>Captured forecasts for browser playback; the browser never estimates probabilities from plotted distance.</summary>
public sealed record PredictionReplayData(string Version, int Seed, int TrainingVisits, int HorizonEvents,
    IReadOnlyList<ReplayParticipant> Participants);

/// <summary>Replays fresh teaching visits against a frozen model and exports the real predictions after each observation.</summary>
public static class PredictionReplay
{
    /// <summary>Builds a separate replay engine so neither these visits nor their endpoints train the evaluated population.</summary>
    public static async Task<PredictionReplayData> CaptureAsync(RelevanceDefinition definition, TransitionModel trained,
        PredictionFixture fixture, CancellationToken cancellationToken = default)
    {
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore(),
            clock: new FixtureClock(), predictor: new FrozenTransitions(trained));
        await PredictionDemo.SaveProductsAsync(engine, fixture.Products);
        var product = fixture.Products.First(p => p.Available);
        var participants = new List<ReplayParticipant>();
        foreach (var (id, name, actions) in new[]
        {
            ("a", "A / Compare, then checkout", new[] { "view", "compare", "cart", "checkout", "purchase" }),
            ("b", "B / Add, then browse", new[] { "view", "cart", "view", "leave" }),
            ("c", "C / Remove, then return", new[] { "view", "compare", "compare", "cart", "remove", "compare", "cart", "cancel" })
        })
        {
            var subject = $"replay-{id}";
            var encoder = new JourneyState();
            var frames = new List<ReplayFrame> { new(null, await engine.PredictAsync(subject, cancellationToken)) };
            for (var index = 0; index < actions.Length; index++)
            {
                var observation = new FixtureEvent($"{subject}-{index}", subject, product.Id, actions[index],
                    FixtureGenerator.Epoch.AddHours(11).AddMinutes(index));
                await PredictionDemo.SaveAsync(engine, observation, encoder, cancellationToken);
                frames.Add(new(actions[index], await engine.PredictAsync(subject, cancellationToken)));
            }
            participants.Add(new(id, name, frames));
        }
        return new("prediction-replay-v1", fixture.Seed, fixture.Training.Count,
            definition.Destinations!.HorizonEvents, participants);
    }
}
