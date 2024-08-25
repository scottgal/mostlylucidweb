using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework.Converters;
using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.EntityFramework;

public class MostlylucidDbContext : DbContext, IMostlylucidDBContext
{
    public MostlylucidDbContext(DbContextOptions<MostlylucidDbContext> contextOptions) : base(contextOptions)
    {
    }

    public DbSet<CommentEntity> Comments { get; set; }
    public DbSet<BlogPostEntity> BlogPosts { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }

    public DbSet<LanguageEntity> Languages { get; set; }


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("mostlylucid");
        
        modelBuilder.Entity<BlogPostEntity>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

            entity.Property(b=>b.UpdatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            
            
            entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(title, '') || ' ' || coalesce(plain_text_content, ''))", stored: true);
            
           entity.HasIndex(b => b.SearchVector)
                .HasMethod("GIN");
            entity.HasMany(b => b.Comments)
                .WithOne(c => c.BlogPostEntity)
                .HasForeignKey(c => c.BlogPostId);

            entity.HasOne(b => b.LanguageEntity)
                .WithMany(l => l.BlogPosts).HasForeignKey(x => x.LanguageId);

            entity.HasMany(b => b.Categories)
                .WithMany(c => c.BlogPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "blogpostcategory",
                    c => c.HasOne<CategoryEntity>().WithMany().HasForeignKey("CategoryId"),
                    b => b.HasOne<BlogPostEntity>().WithMany().HasForeignKey("BlogPostId")
                );
        });

        modelBuilder.Entity<CommentEntity>(entity =>
        {
            entity.HasIndex(x => x.Slug);
            entity.HasIndex(x => x.Date);
            entity.HasIndex(x => x.BlogPostId);
            entity.HasIndex(x => x.Moderated);

            entity.HasOne(c => c.BlogPostEntity)
                .WithMany(b => b.Comments)
                .HasForeignKey(c => c.BlogPostId);
        });
        
        modelBuilder.Entity<LanguageEntity>(entity =>
        {
            entity.HasMany(l => l.BlogPosts)
                .WithOne(b => b.LanguageEntity);
        });

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
            entity.HasKey(c => c.Id); // Assuming Category has a primary key named Id

            entity.HasMany(c => c.BlogPosts)
                .WithMany(b => b.Categories)
                .UsingEntity<Dictionary<string, object>>(
                    "blogpostcategory",
                    b => b.HasOne<BlogPostEntity>().WithMany().HasForeignKey("BlogPostId"),
                    c => c.HasOne<CategoryEntity>().WithMany().HasForeignKey("CategoryId")
                );
        });
    }
}