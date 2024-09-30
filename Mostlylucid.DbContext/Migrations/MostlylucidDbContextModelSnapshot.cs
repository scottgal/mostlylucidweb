﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mostlylucid.DbContext.EntityFramework;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Mostlylucid.Migrations
{
    [DbContext(typeof(MostlylucidDbContext))]
    partial class MostlylucidDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("mostlylucid")
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("EmailSubscription_Category", b =>
                {
                    b.Property<int>("CategoryId")
                        .HasColumnType("integer");

                    b.Property<int>("EmailSubscriptionId")
                        .HasColumnType("integer");

                    b.HasKey("CategoryId", "EmailSubscriptionId");

                    b.HasIndex("EmailSubscriptionId");

                    b.ToTable("EmailSubscription_Category", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.BlogPostEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ContentHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("HtmlContent")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("LanguageId")
                        .HasColumnType("integer");

                    b.Property<string>("Markdown")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PlainTextContent")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("PublishedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<NpgsqlTsVector>("SearchVector")
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("tsvector")
                        .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", true);

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("WordCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ContentHash")
                        .IsUnique();

                    b.HasIndex("LanguageId");

                    b.HasIndex("PublishedDate");

                    b.HasIndex("SearchVector");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("SearchVector"), "GIN");

                    b.HasIndex("Slug", "LanguageId");

                    b.ToTable("BlogPosts", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.CategoryEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .HasAnnotation("Npgsql:TsVectorConfig", "english");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("Name"), "GIN");

                    b.ToTable("Categories", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.CommentClosure", b =>
                {
                    b.Property<int>("AncestorId")
                        .HasColumnType("integer")
                        .HasColumnName("ancestor_id");

                    b.Property<int>("DescendantId")
                        .HasColumnType("integer")
                        .HasColumnName("descendant_id");

                    b.Property<int>("Depth")
                        .HasColumnType("integer")
                        .HasColumnName("depth");

                    b.HasKey("AncestorId", "DescendantId");

                    b.HasIndex("DescendantId");

                    b.ToTable("comment_closures", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.CommentEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Author")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("HtmlContent")
                        .HasColumnType("text");

                    b.Property<int?>("ParentCommentId")
                        .HasColumnType("integer")
                        .HasColumnName("parent_comment_id");

                    b.Property<int>("PostId")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Author");

                    b.HasIndex("ParentCommentId");

                    b.HasIndex("PostId");

                    b.ToTable("Comments", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.EmailSubscriptionEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedDate")
                        .HasMaxLength(100)
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Day")
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.Property<int?>("DayOfMonth")
                        .HasColumnType("integer");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasMaxLength(2)
                        .HasColumnType("character varying(2)");

                    b.Property<DateTimeOffset?>("LastSent")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("SubscriptionType")
                        .HasColumnType("integer");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.HasIndex("Email");

                    b.HasIndex("Token")
                        .IsUnique();

                    b.ToTable("EmailSubscriptions", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.EmailSubscriptionSendLogEntity", b =>
                {
                    b.Property<string>("SubscriptionType")
                        .HasColumnType("varchar(24)");

                    b.Property<DateTimeOffset>("LastSent")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("SubscriptionType");

                    b.ToTable("EmailSubscriptionSendLogs", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.LanguageEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Languages", "mostlylucid");
                });

            modelBuilder.Entity("blogpostcategory", b =>
                {
                    b.Property<int>("BlogPostId")
                        .HasColumnType("integer");

                    b.Property<int>("CategoryId")
                        .HasColumnType("integer");

                    b.HasKey("BlogPostId", "CategoryId");

                    b.HasIndex("CategoryId");

                    b.ToTable("blogpostcategory", "mostlylucid");
                });

            modelBuilder.Entity("EmailSubscription_Category", b =>
                {
                    b.HasOne("Mostlylucid.Shared.Entities.CategoryEntity", null)
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Mostlylucid.Shared.Entities.EmailSubscriptionEntity", null)
                        .WithMany()
                        .HasForeignKey("EmailSubscriptionId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.BlogPostEntity", b =>
                {
                    b.HasOne("Mostlylucid.Shared.Entities.LanguageEntity", "LanguageEntity")
                        .WithMany("BlogPosts")
                        .HasForeignKey("LanguageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LanguageEntity");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.CommentClosure", b =>
                {
                    b.HasOne("Mostlylucid.Shared.Entities.CommentEntity", "Ancestor")
                        .WithMany("Descendants")
                        .HasForeignKey("AncestorId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Mostlylucid.Shared.Entities.CommentEntity", "Descendant")
                        .WithMany("Ancestors")
                        .HasForeignKey("DescendantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ancestor");

                    b.Navigation("Descendant");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.CommentEntity", b =>
                {
                    b.HasOne("Mostlylucid.Shared.Entities.CommentEntity", "ParentComment")
                        .WithMany()
                        .HasForeignKey("ParentCommentId");

                    b.HasOne("Mostlylucid.Shared.Entities.BlogPostEntity", "Post")
                        .WithMany("Comments")
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentComment");

                    b.Navigation("Post");
                });

            modelBuilder.Entity("blogpostcategory", b =>
                {
                    b.HasOne("Mostlylucid.Shared.Entities.BlogPostEntity", null)
                        .WithMany()
                        .HasForeignKey("BlogPostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Mostlylucid.Shared.Entities.CategoryEntity", null)
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.BlogPostEntity", b =>
                {
                    b.Navigation("Comments");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.CommentEntity", b =>
                {
                    b.Navigation("Ancestors");

                    b.Navigation("Descendants");
                });

            modelBuilder.Entity("Mostlylucid.Shared.Entities.LanguageEntity", b =>
                {
                    b.Navigation("BlogPosts");
                });
#pragma warning restore 612, 618
        }
    }
}