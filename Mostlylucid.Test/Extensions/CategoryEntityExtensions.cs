using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.Test.Extensions;

public class CategoryEntityExtensions
{
    private static readonly string[] Categories =
    {
        "Category 1",
        "Category 2",
        "Category 3",
        "Category 4",
        "Category 5",
        "Category 6",
        "Category 7",
        "Category 8",
        "Category 9",
        "Category 10"
    };

    public static CategoryEntity GetCategoryEntity(string name)
    {
        var index = Array.IndexOf(Categories, name);
        return new CategoryEntity
        {
            Id = index + 1,
            Name = name
        };
    }

    public static List<CategoryEntity> GetCategoryEntities()
    {
        return Categories.Select((x, i) => new CategoryEntity
        {
            Id = i + 1,
            Name = x
        }).ToList();
    }

    public static List<CategoryEntity> GetCategoryEntities(int count)
    {
        var langs = Categories.Take(count);
        return langs.Select((x, i) => new CategoryEntity
        {
            Id = i + 1,
            Name = x
        }).ToList();
    }
}