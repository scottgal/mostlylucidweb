namespace Mostlylucid.OpenSearch;

public class BaseService
{
    protected string GetBlogIndexName(string language) => $"mostlylucid-blog-{language}";
}