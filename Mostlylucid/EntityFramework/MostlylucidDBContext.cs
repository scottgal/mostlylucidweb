using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.EntityFramework;

public class MostlylucidDBContext : DbContext
{
    public MostlylucidDBContext(DbContextOptions<MostlylucidDBContext> contextOptions) 
        : base(contextOptions)
    {
    }

    public DbSet<Comments> Comments { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.ContentHash).IsUnique();

            entity.HasMany(b => b.Comments)
                .WithOne(c => c.BlogPost)
                .HasForeignKey(c => c.BlogPostId);

            entity.HasMany(b => b.Categories)
                .WithMany(c => c.BlogPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    b => b.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                    c => c.HasOne<BlogPost>().WithMany().HasForeignKey("BlogPostId")
                );
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);  // Assuming Category has a primary key named Id
        });
    }
}