﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mostlylucid.EntityFramework;
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
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.BlogPostEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ContentHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("content_hash");

                    b.Property<string>("HtmlContent")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("html_content");

                    b.Property<int>("LanguageId")
                        .HasColumnType("integer")
                        .HasColumnName("language_id");

                    b.Property<string>("Markdown")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("markdown");

                    b.Property<string>("PlainTextContent")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("plain_text_content");

                    b.Property<DateTimeOffset>("PublishedDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("published_date");

                    b.Property<NpgsqlTsVector>("SearchVector")
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("tsvector")
                        .HasColumnName("search_vector")
                        .HasComputedColumnSql("to_tsvector('english', coalesce(title, '') || ' ' || coalesce(plain_text_content, ''))", true);

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("slug");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("title");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_date")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("WordCount")
                        .HasColumnType("integer")
                        .HasColumnName("word_count");

                    b.HasKey("Id");

                    b.HasIndex("ContentHash")
                        .IsUnique();

                    b.HasIndex("LanguageId");

                    b.HasIndex("PublishedDate");

                    b.HasIndex("SearchVector");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("SearchVector"), "GIN");

                    b.HasIndex("Slug", "LanguageId");

                    b.ToTable("blogposts", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CategoryEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .HasAnnotation("Npgsql:TsVectorConfig", "english");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("Name"), "GIN");

                    b.ToTable("categories", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentClosure", b =>
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

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Author")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("author");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)")
                        .HasColumnName("content");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("HtmlContent")
                        .HasColumnType("text")
                        .HasColumnName("html_content");

                    b.Property<int?>("ParentCommentId")
                        .HasColumnType("integer");

                    b.Property<int>("PostId")
                        .HasColumnType("integer")
                        .HasColumnName("post_id");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.HasKey("Id");

                    b.HasIndex("Author");

                    b.HasIndex("ParentCommentId");

                    b.HasIndex("PostId");

                    b.ToTable("comments", "mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.LanguageEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.ToTable("languages", "mostlylucid");
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

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.BlogPostEntity", b =>
                {
                    b.HasOne("Mostlylucid.EntityFramework.Models.LanguageEntity", "LanguageEntity")
                        .WithMany("BlogPosts")
                        .HasForeignKey("LanguageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LanguageEntity");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentClosure", b =>
                {
                    b.HasOne("Mostlylucid.EntityFramework.Models.CommentEntity", "Ancestor")
                        .WithMany("Descendants")
                        .HasForeignKey("AncestorId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Mostlylucid.EntityFramework.Models.CommentEntity", "Descendant")
                        .WithMany("Ancestors")
                        .HasForeignKey("DescendantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ancestor");

                    b.Navigation("Descendant");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentEntity", b =>
                {
                    b.HasOne("Mostlylucid.EntityFramework.Models.CommentEntity", "ParentComment")
                        .WithMany()
                        .HasForeignKey("ParentCommentId");

                    b.HasOne("Mostlylucid.EntityFramework.Models.BlogPostEntity", "Post")
                        .WithMany("Comments")
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentComment");

                    b.Navigation("Post");
                });

            modelBuilder.Entity("blogpostcategory", b =>
                {
                    b.HasOne("Mostlylucid.EntityFramework.Models.BlogPostEntity", null)
                        .WithMany()
                        .HasForeignKey("BlogPostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Mostlylucid.EntityFramework.Models.CategoryEntity", null)
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.BlogPostEntity", b =>
                {
                    b.Navigation("Comments");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentEntity", b =>
                {
                    b.Navigation("Ancestors");

                    b.Navigation("Descendants");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.LanguageEntity", b =>
                {
                    b.Navigation("BlogPosts");
                });
#pragma warning restore 612, 618
        }
    }
}
