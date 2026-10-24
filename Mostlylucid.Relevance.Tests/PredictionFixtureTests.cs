using System.Text.Json;
using Mostlylucid.Relevance.PredictDemo;

namespace Mostlylucid.Relevance.Tests;

public sealed class PredictionFixtureTests
{
    [Fact]
    public void FixtureIsRepeatableDisjointAndTimeOrderedWithCensoredFeedback()
    {
        var fixture = FixtureGenerator.Generate(814, 60, 100, 60);
        Assert.Equal(JsonSerializer.Serialize(fixture), JsonSerializer.Serialize(FixtureGenerator.Generate(814, 60, 100, 60)));
        Assert.Empty(fixture.Training.Select(j => j.User.Id).Intersect(fixture.Holdout.Select(j => j.User.Id)));
        Assert.True(fixture.Training.SelectMany(j => j.Events).Max(e => e.At) < fixture.Holdout.SelectMany(j => j.Events).Min(e => e.At));
        Assert.Contains(fixture.Training, j => j.Outcome is null);
        foreach (var journey in fixture.Training.Concat(fixture.Holdout))
        {
            Assert.True(journey.Events.Zip(journey.Events.Skip(1)).All(pair => pair.First.At < pair.Second.At));
            Assert.All(journey.Events, e => Assert.Contains(fixture.Products, p => p.Id == e.Candidate && p.Available));
            Assert.Equal(journey.Outcome is not null, journey.Events.Any(e => new[] {"purchase", "cancel", "leave"}.Contains(e.Type)));
        }
    }

    [Fact]
    public void PrefixEncodingPreservesProgressionAndDoesNotReadFutureOutcomes()
    {
        var considered = new JourneyState();
        Assert.Equal("browsing", considered.Observe("view"));
        Assert.Equal("comparing", considered.Observe("compare"));
        Assert.Equal("considered_cart", considered.Observe("cart"));
        Assert.Equal("cart_removed", considered.Observe("remove"));
        Assert.Equal("reconsidering", considered.Observe("compare"));
        Assert.Equal("returning_cart", considered.Observe("cart"));
        var impulsive = new JourneyState();
        impulsive.Observe("view");
        Assert.Equal("impulsive_cart", impulsive.Observe("cart"));
        Assert.Equal("post_cart_browsing", impulsive.Observe("view"));
        var research = new JourneyState();research.Observe("compare");research.Observe("compare");
        Assert.Equal("research_cart", research.Observe("cart"));
        Assert.Throws<ArgumentException>(() => research.Observe("unsupported"));
    }

    [Fact]
    public void OutcomeWindowDistinguishesUnresolvedFromUnobservedFeedback()
    {
        FixtureJourney Journey(params string[] actions) => new(new("s", "Fake", "home", 100), actions.Select((type,i)=>
            new FixtureEvent($"e{i}","s","p",type,FixtureGenerator.Epoch.AddSeconds(i))).ToArray(), null);
        Assert.Equal("purchase", OutcomeEvaluation.Target(Journey("cart","checkout","purchase"),0,4));
        Assert.Null(OutcomeEvaluation.Target(Journey("cart","view"),0,4));
        Assert.Equal("unresolved", OutcomeEvaluation.Target(Journey("cart","view","compare","view","view","purchase"),0,4));
        Assert.Equal(0, OutcomeEvaluation.Brier(new Dictionary<string,double>{{"purchase",1}},"purchase"));
        Assert.Equal(2, OutcomeEvaluation.Brier(new Dictionary<string,double>{{"cancel",1}},"purchase"));
    }

    [Fact]
    public async Task FrozenEvaluationCannotLearnFromHeldOutOutcomeCounts()
    {
        var definition = PredictionDemo.Definition().Destinations!;
        definition.MinTransitions = 1;
        var training = new TransitionModel {Counts = new() {["considered_cart"] = new() {["purchase"] = 10}}};
        var frozen = new FrozenTransitions(training);
        training.Counts["considered_cart"]["cancel"] = 1000;
        var future = new TransitionModel {Counts = new() {["considered_cart"] = new() {["cancel"] = 1000}}};
        var prediction = await frozen.PredictAsync(new("held-out", new() {LastState="considered_cart"},future,definition));
        Assert.Equal(1,prediction.Outcomes.Single(o=>o.Destination=="purchase").Probability);
        Assert.Equal(1,prediction.Outcomes.Sum(o=>o.Probability)+prediction.RemainingProbability,10);
        Assert.False(prediction.IsCalibrated);
    }

    [Fact]
    public void CopyOverlayChangesDisplayCopyWithoutChangingObservedFeaturesOrLabels()
    {
        var products = FixtureGenerator.Generate(814,12,3,1).Products;
        var json = """{"model":"fake-model","promptVersion":"catalogue-copy-v1","products":[{"id":"p-0001","name":"New copy","description":"New description"}]}""";
        var updated = CatalogueCopy.Apply(products,json);
        Assert.Equal("New copy",updated[0].Name);
        Assert.Equal(products[0],updated[0] with {Name=products[0].Name,Description=products[0].Description});
        Assert.Equal(products.Skip(1),updated.Skip(1));
        Assert.Throws<ArgumentException>(()=>CatalogueCopy.Apply(products,json.Replace("\"name\":\"New copy\"","\"price\":1")));
        Assert.Throws<ArgumentException>(()=>CatalogueCopy.Apply(products,json.Replace("p-0001","unknown")));
    }

    [Theory]
    [InlineData(814)]
    [InlineData(129)]
    [InlineData(2026)]
    public async Task LearnedPredictionBeatsThePopulationPriorAcrossIndependentSyntheticFixtures(int seed)
    {
        var fixture=FixtureGenerator.Generate(seed,60,400,150);
        var definition=PredictionDemo.Definition();var capture=new CaptureTransitions();
        // Separate stores: evaluation receives no training state and its own updates cannot alter the frozen counts.
        await using var training=new RelevanceEngine(definition,new MemoryRelevanceStore(),clock:new FixtureClock(),predictor:capture);
        await PredictionDemo.SaveProductsAsync(training,fixture.Products);
        foreach(var journey in fixture.Training)
        {
            var encoder=new JourneyState();
            foreach(var observation in journey.Events)await PredictionDemo.SaveAsync(training,observation,encoder);
        }
        await training.PredictAsync("snapshot");
        await using var evaluator=new RelevanceEngine(definition,new MemoryRelevanceStore(),clock:new FixtureClock(),predictor:new FrozenTransitions(capture.Snapshot));
        await PredictionDemo.SaveProductsAsync(evaluator,fixture.Products);
        var evaluated=await OutcomeEvaluation.RunAsync(evaluator,fixture,4);
        Assert.True(evaluated.Summary.ModelBrier < evaluated.Summary.PriorBrier, $"seed={seed}: {evaluated.Summary}");
        Assert.Equal(150,evaluated.Summary.Scored+evaluated.Summary.Censored+evaluated.Summary.WithoutCart);
        Assert.All(evaluated.Rows,row=>Assert.Equal(1,row.Probabilities.Values.Sum(),10));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnObservedOutcomeUpdatesTheNextForecastExactlyOnce(bool sqlite)
    {
        var definition=PredictionDemo.Definition();definition.Destinations!.MinTransitions=1;
        IRelevanceStore store=sqlite?new Mostlylucid.Relevance.Sqlite.SqliteRelevanceStore(Path.Combine(Path.GetTempPath(),$"predict-update-{Guid.NewGuid():N}.db")):new MemoryRelevanceStore();
        await using var engine=new RelevanceEngine(definition,store,clock:new FixtureClock());
        for(var i=0;i<3;i++)
        {
            await engine.SaveAsync("events",new {id=$"start-{i}",subject=$"history-{i}",type="checkout",state="checkout"});
            await engine.SaveAsync("events",new {id=$"end-{i}",subject=$"history-{i}",type="purchase",state="purchase"});
        }
        await engine.SaveAsync("events",new {id="current",subject="current",type="checkout",state="checkout"});
        var before=await engine.PredictAsync("current");
        var outcome=new {id="cancel",subject="current",type="cancel",state="cancel"};
        await engine.SaveAsync("events",outcome);
        Assert.True((await engine.SaveAsync("events",outcome)).Duplicate);
        Assert.Equal("observed_destination",(await engine.PredictAsync("current")).Status);
        await engine.SaveAsync("events",new {id="next",subject="next",type="checkout",state="checkout"});
        var after=await engine.PredictAsync("next");
        Assert.Equal(1,before.Outcomes.Single(p=>p.Destination=="purchase").Probability);
        Assert.Equal(.75,after.Outcomes.Single(p=>p.Destination=="purchase").Probability,10);
        Assert.Equal(4,after.SupportingTransitions);
    }

    [Fact]
    public async Task BrowserReplayCapturesRealPrefixForecastsWithoutUpdatingItsTrainingPopulation()
    {
        var fixture = FixtureGenerator.Generate(814, 12, 3, 1);
        var counts = new TransitionModel { Counts = new()
        {
            ["considered_cart"] = new() { ["checkout"] = 100 },
            ["checkout"] = new() { ["purchase"] = 80, ["cancel"] = 20 }
        } };
        var before = JsonSerializer.Serialize(counts);
        var replay = await PredictionReplay.CaptureAsync(PredictionDemo.Definition(), counts, fixture);
        Assert.Equal(before, JsonSerializer.Serialize(counts));
        var frames = replay.Participants[0].Frames;
        Assert.Equal("no_state", frames[0].Prediction.Status);
        Assert.Equal("considered_cart", frames[3].Prediction.CurrentState);
        Assert.Equal(.8, frames[3].Prediction.Outcomes.Single(o => o.Destination == "purchase").Probability, 10);
        Assert.Equal("observed_destination", frames[^1].Prediction.Status);
        Assert.Equal(1, frames[^1].Prediction.Outcomes.Single(o => o.Destination == "purchase").Probability);
        Assert.All(replay.Participants.SelectMany(p => p.Frames), f =>
            Assert.Equal(1, f.Prediction.Outcomes.Sum(o => o.Probability) + f.Prediction.RemainingProbability, 10));
    }

}
