namespace Mostlylucid.Models.Blog;

public record SortOptions
{
    public OrderPostsBy OrderBy { get; init; }
    public SortOrder SortOrder { get; init; }
}

public enum OrderPostsBy
{
    Default,
    Popularity,
    Published,
    Title,
    PopularityInCountry,
    PopularityInRegion,
    PopularityInContinent
}

public enum SortOrder
{
    Ascending,
    Descending
}