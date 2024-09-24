using System.ComponentModel.DataAnnotations;

namespace Mostlylucid.Models.Blog;

public record SortOptions
{
    public OrderPostsBy OrderBy { get; init; }
    public SortOrder SortOrder { get; init; }
}

public enum OrderPostsBy
{
    [Display(Name = "Published Date")]
    Default,
    [Display(Name = "Popularity")]
    Popularity,
    [Display(Name = "Title")]
    Title,
    [Display(Name = "My Country")]
    PopularityInCountry,
    [Display(Name = "My Region")]
    PopularityInRegion,
    [Display(Name = "My Continent")]
    PopularityInContinent
}

public enum SortOrder
{
    Ascending,
    Descending
}