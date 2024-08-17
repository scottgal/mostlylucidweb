using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.Blog.EntityFramework;

public class EFBaseService
{
    protected  readonly MostlylucidDbContext Context;

    public EFBaseService(MostlylucidDbContext context)
    {
        Context = context;
    }

    public async Task<List<string>> GetCategories() => await Context.Categories.Select(x => x.Name).ToListAsync();
    
    protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);

}