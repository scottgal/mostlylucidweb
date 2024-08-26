using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.EntityFramework;

public interface IMostlylucidDBContext
{
     DbSet<CommentEntity> Comments { get; set; }
    
    public DbSet<CommentClosure> CommentClosures { get; set; }
    public DbSet<BlogPostEntity> BlogPosts { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }

    public DbSet<LanguageEntity> Languages { get; set; }
    
    public DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}