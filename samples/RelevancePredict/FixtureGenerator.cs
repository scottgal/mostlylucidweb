using Bogus;

namespace Mostlylucid.Relevance.PredictDemo;

/// <summary>Invented catalogue metadata; only category, price and swatch become coordinates.</summary>
public sealed record FixtureProduct(string Id, string Name, string Category, decimal Price, bool Available, string Swatch, string? Description);
/// <summary>A fake user for the fixture; no real customer identifiers or contact details.</summary>
public sealed record FixtureUser(string Id, string Name, string Category, decimal Budget);
/// <summary>A transport observation; State is deliberately derived later from the prefix, not supplied by the generator.</summary>
public sealed record FixtureEvent(string Id, string Subject, string Candidate, string Type, DateTimeOffset At);
/// <summary>A synthetic episode. A null outcome denotes censored feedback, not a failed purchase.</summary>
public sealed record FixtureJourney(FixtureUser User, IReadOnlyList<FixtureEvent> Events, string? Outcome);
/// <summary>Versioned, reproducible fake data. Outcomes are simulator labels, never production evidence.</summary>
public sealed record PredictionFixture(int Seed, string Version, IReadOnlyList<FixtureProduct> Products,
    IReadOnlyList<FixtureJourney> Training, IReadOnlyList<FixtureJourney> Holdout);

/// <summary>Uses independent local random streams so editing prose does not silently alter simulated behaviour.</summary>
public static class FixtureGenerator
{
    public const string Version = "bogus-shop-v2";
    public static readonly DateTimeOffset Epoch = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] Categories = ["outdoors", "electronics", "home"];
    private static readonly string[][] Kinds = [["Walking boots", "Trail backpack", "Rain shell", "Camp flask"],
        ["Headphones", "Keyboard", "Pocket speaker", "Travel charger"], ["Desk lamp", "Stoneware mug", "Kettle", "Storage basket"]];
    private static readonly string[] Swatches = ["#9b563a", "#456859", "#354456", "#c0a783"];

    /// <summary>Builds separate earlier training and later holdout populations with fixed timestamps.</summary>
    public static PredictionFixture Generate(int seed = 814, int products = 240, int training = 1000, int holdout = 400)
    {
        if (products is < 12 or > 5000 || training is < 3 or > 5000 || holdout is < 1 or > 2000)
            throw new ArgumentOutOfRangeException(nameof(products), "Use 12–5000 products, 3–5000 training visits and 1–2000 holdout visits.");
        var catalogueRandom = new Random(seed);
        var copy = new Faker("en_GB") { Random = new Randomizer(seed ^ 0x7351) };
        var catalogue = Enumerable.Range(0, products).Select(index =>
        {
            var category = index % Categories.Length;
            var kind = Kinds[category][catalogueRandom.Next(Kinds[category].Length)];
            return new FixtureProduct($"p-{index + 1:0000}", $"{copy.Commerce.ProductAdjective()} {kind}", Categories[category],
                catalogueRandom.Next(18, 281), index < 3 || catalogueRandom.NextDouble() > .12, Swatches[catalogueRandom.Next(Swatches.Length)],
                catalogueRandom.NextDouble() < .2 ? null : $"{kind} for {Categories[category]} use. This synthetic listing supplies a price and colour swatch; material and size details have not been provided.");
        }).ToArray();
        return new(seed, Version, catalogue, Journeys(seed ^ 0x1234, training, "train", Epoch.AddHours(8), catalogue),
            Journeys(seed ^ 0x5678, holdout, "test", Epoch.AddHours(10), catalogue));
    }

    private static FixtureJourney[] Journeys(int seed, int count, string partition, DateTimeOffset start, FixtureProduct[] catalogue)
    {
        var random = new Random(seed);
        var names = new Faker("en_GB") { Random = new Randomizer(seed ^ 0x3917) };
        return Enumerable.Range(0, count).Select(index =>
        {
            // Intent exists only inside the simulator. It is never a predictor input or a stored user field.
            var intent = random.NextDouble();
            var user = new FixtureUser($"{partition}-{index + 1:00000}", names.Name.FirstName(), Categories[random.Next(3)], random.Next(60, 301));
            var candidates = catalogue.Where(p => p.Available && p.Category == user.Category && p.Price <= user.Budget).ToArray();
            if (candidates.Length == 0) candidates = catalogue.Where(p => p.Available && p.Category == user.Category).ToArray();
            string[] path;
            double purchase, cancel;
            if (intent < .4) { path = ["view", "compare", "cart", "checkout"]; purchase = .85; cancel = .08; }
            else if (intent < .75) { path = ["view", "cart", "view"]; purchase = .2; cancel = .25; }
            else { path = ["view", "compare", "compare", "cart", "remove", "compare", "cart"]; purchase = .35; cancel = .3; }
            var draw = random.NextDouble();
            string? outcome = draw < purchase ? "purchase" : draw < purchase + cancel ? "cancel" : "leave";
            var types = path.Append(outcome).ToArray();
            if (random.NextDouble() < .12)
            {
                types = path.Take(random.Next(1, path.Length + 1)).ToArray();
                outcome = null; // We stop observing; we do not relabel an unknown future as 'leave'.
            }
            var at = start.AddSeconds(index);
            var events = types.Select((type, eventIndex) =>
            {
                at = at.AddSeconds(random.Next(10, 61));
                return new FixtureEvent($"{user.Id}-{eventIndex:00}", user.Id, candidates[random.Next(candidates.Length)].Id, type!, at);
            }).ToArray();
            return new FixtureJourney(user, events, outcome);
        }).ToArray();
    }
}

/// <summary>A prefix-only encoder: it can read only events already observed in this visit.</summary>
public sealed class JourneyState
{
    private int comparisons;
    private bool cartSeen;
    private bool removed;
    /// <summary>Maps an action and remembered prefix to the configured state vocabulary; never accepts an outcome label separately.</summary>
    public string Observe(string type)
    {
        if (type == "compare") comparisons++;
        if (type == "remove") removed = true;
        var state = type switch
        {
            "view" => cartSeen ? "post_cart_browsing" : "browsing",
            "compare" => removed ? "reconsidering" : comparisons > 1 ? "repeated_comparison" : "comparing",
            "cart" => removed ? "returning_cart" : comparisons > 1 ? "research_cart" : comparisons > 0 ? "considered_cart" : "impulsive_cart",
            "remove" => "cart_removed", "checkout" => "checkout",
            "purchase" or "cancel" or "leave" => type,
            _ => throw new ArgumentException($"Unknown action '{type}'.")
        };
        if (type == "cart") cartSeen = true;
        return state;
    }
}
