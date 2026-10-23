using Mostlylucid.AudienceSegmentation.Demo.Models;

namespace Mostlylucid.Relevance.Demo;

// Reuses the product model from the existing audience-segmentation demo, without its remote dependencies.
public sealed class DemoProduct : Product
{
    public string Colour { get; set; } = "";
    public string Swatch { get; set; } = "";
    public string Shape { get; set; } = "";
    public bool Available { get; set; } = true;
}

public sealed class DemoCatalog
{
    public IReadOnlyList<DemoProduct> Products { get; } = Generate();
    public DemoProduct Get(string id) => Products.FirstOrDefault(p => p.Id == id) ?? throw new ArgumentException("Unknown product.");

    private static IReadOnlyList<DemoProduct> Generate()
    {
        var kinds = new[]
        {
            ("outdoors", "Trail backpack", "pack", 62), ("outdoors", "Walking boots", "boot", 98),
            ("outdoors", "Rain shell", "coat", 112), ("outdoors", "Camp flask", "bottle", 28),
            ("outdoors", "Day sling", "pack", 42), ("outdoors", "Trail cap", "cap", 24),
            ("outdoors", "Field jacket", "coat", 138),
            ("electronics", "Studio headphones", "headphones", 88), ("electronics", "Pocket speaker", "speaker", 56),
            ("electronics", "Travel charger", "speaker", 32), ("electronics", "Desk headphones", "headphones", 122),
            ("electronics", "Portable radio", "speaker", 74), ("electronics", "Cable organiser", "pack", 18),
            ("electronics", "Compact speaker", "speaker", 45),
            ("home", "Stoneware mug", "mug", 22), ("home", "Pour-over kettle", "kettle", 54),
            ("home", "Desk lamp", "lamp", 68), ("home", "Storage basket", "pack", 34),
            ("home", "Bedside lamp", "lamp", 58), ("home", "Coffee flask", "bottle", 26),
            ("home", "Breakfast cup", "mug", 18)
        };
        var colours = new[] { ("Russet", "#9b563a"), ("Forest", "#456859"), ("Ink", "#354456"), ("Sand", "#c0a783") };
        var products = new List<DemoProduct>();
        foreach (var (category, name, shape, price) in kinds)
            foreach (var (colour, swatch) in colours)
            {
                products.Add(new DemoProduct
                {
                    Id = $"product-{products.Count + 1:000}", Name = name, Category = category,
                    Price = price + products.Count % 4 * 3, Colour = colour, Swatch = swatch, Shape = shape,
                    Description = $"A considered everyday {name.ToLowerInvariant()} in {colour.ToLowerInvariant()}. Built for repeat use, with simple details and a practical finish.",
                    Tags = [category, colour.ToLowerInvariant(), shape], Available = products.Count % 19 != 0
                });
            }
        return products;
    }
}
