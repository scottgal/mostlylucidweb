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
    
    public DbSet<CommentClosure> CommentClosures { get; set; }
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
           // Configure the CommentClosure entity
           modelBuilder.Entity<CommentClosure>()
               .HasKey(cc => new { cc.AncestorId, cc.DescendantId });

           modelBuilder.Entity<CommentClosure>()
               .HasOne(cc => cc.Ancestor)
               .WithMany(c => c.Descendants)
               .HasForeignKey(cc => cc.AncestorId)
               .OnDelete(DeleteBehavior.Restrict);

           modelBuilder.Entity<CommentClosure>()
               .HasOne(cc => cc.Descendant)
               .WithMany(c => c.Ancestors)
               .HasForeignKey(cc => cc.DescendantId)
               .OnDelete(DeleteBehavior.Cascade);
           
           modelBuilder.Entity<CommentEntity>(entity =>
           {
               entity.HasKey(c => c.Id);  // Primary key

               entity.Property(c => c.Content)
                   .IsRequired()
                   .HasMaxLength(1000);  // Example constraint on content length

               entity.Property(x=>x.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
               entity.Property(x=>x.Author).IsRequired().HasMaxLength(200);
               entity.Property(c => c.CreatedAt)
                   .IsRequired();  // Ensure the creation date is required

               // Configure the relationship between Comment and Post (optional)
               entity.HasOne(c => c.Post)
                   .WithMany(p => p.Comments)
                   .HasForeignKey(c => c.PostId)
                   .OnDelete(DeleteBehavior.Cascade);  // Optional, cascade delete on post deletion

               entity.HasIndex(c => c.Author);
           });

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