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

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Avatar")
                        .HasColumnType("text")
                        .HasColumnName("avatar");

                    b.Property<int>("BlogPostId")
                        .HasColumnType("integer")
                        .HasColumnName("blog_post_id");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("content");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("email");

                    b.Property<bool>("Moderated")
                        .HasColumnType("boolean")
                        .HasColumnName("moderated");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("slug");

                    b.HasKey("Id");

                    b.HasIndex("BlogPostId");

                    b.HasIndex("Date");

                    b.HasIndex("Moderated");

                    b.HasIndex("Slug");

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

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentEntity", b =>
                {
                    b.HasOne("Mostlylucid.EntityFramework.Models.BlogPostEntity", "BlogPostEntity")
                        .WithMany("Comments")
                        .HasForeignKey("BlogPostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BlogPostEntity");
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

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.LanguageEntity", b =>
                {
                    b.Navigation("BlogPosts");
                });
#pragma warning restore 612, 618
        }
    }
}