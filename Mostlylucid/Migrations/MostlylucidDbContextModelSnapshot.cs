﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mostlylucid.EntityFramework;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                .HasDefaultSchema("Mostlylucid")
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BlogPostCategory", b =>
                {
                    b.Property<int>("BlogPostId")
                        .HasColumnType("integer");

                    b.Property<int>("CategoryId")
                        .HasColumnType("integer");

                    b.HasKey("BlogPostId", "CategoryId");

                    b.HasIndex("CategoryId");

                    b.ToTable("BlogPostCategory", "Mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.BlogPostEntity", b =>
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

                    b.Property<string>("PlainTextContent")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("PublishedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("WordCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ContentHash")
                        .IsUnique();

                    b.HasIndex("LanguageId");

                    b.HasIndex("PublishedDate");

                    b.HasIndex("Slug", "LanguageId");

                    b.ToTable("BlogPosts", "Mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CategoryEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Categories", "Mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.CommentEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Avatar")
                        .HasColumnType("text");

                    b.Property<int>("BlogPostId")
                        .HasColumnType("integer");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("Moderated")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("BlogPostId");

                    b.HasIndex("Date");

                    b.HasIndex("Moderated");

                    b.HasIndex("Slug");

                    b.ToTable("Comments", "Mostlylucid");
                });

            modelBuilder.Entity("Mostlylucid.EntityFramework.Models.LanguageEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Languages", "Mostlylucid");
                });

            modelBuilder.Entity("BlogPostCategory", b =>
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
