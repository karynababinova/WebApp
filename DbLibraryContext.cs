using System;
using System.Collections.Generic;
using LibraryDomain.Model;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure;

public partial class DbLibraryContext : DbContext
{
    public DbLibraryContext()
    {
    }

    public DbLibraryContext(DbContextOptions<DbLibraryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bookmark> Bookmarks { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<ContentRating> ContentRatings { get; set; }

    public virtual DbSet<Fanfic> Fanfics { get; set; }

    public virtual DbSet<FanficStatus> FanficStatuses { get; set; }

    public virtual DbSet<FanficTag> FanficTags { get; set; }

    public virtual DbSet<Like> Likes { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=fanfic_db;Username=karyna.babinova;Password=12345678qaz");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bookmarks_pkey");

            entity.ToTable("bookmarks");

            entity.HasIndex(e => new { e.UserId, e.FanficId }, "uq_bookmark").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FanficId).HasColumnName("fanfic_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Fanfic).WithMany(p => p.Bookmarks)
                .HasForeignKey(d => d.FanficId)
                .HasConstraintName("bookmarks_fanfic_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Bookmarks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("bookmarks_user_id_fkey");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chapters_pkey");

            entity.ToTable("chapters");

            entity.HasIndex(e => new { e.FanficId, e.ChapterNumber }, "uq_chapter_number").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChapterNumber).HasColumnName("chapter_number");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FanficId).HasColumnName("fanfic_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Fanfic).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.FanficId)
                .HasConstraintName("chapters_fanfic_id_fkey");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comments_pkey");

            entity.ToTable("comments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FanficId).HasColumnName("fanfic_id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.ParentCommentId).HasColumnName("parent_comment_id");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Comments)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comments_chapter_id_fkey");

            entity.HasOne(d => d.Fanfic).WithMany(p => p.Comments)
                .HasForeignKey(d => d.FanficId)
                .HasConstraintName("comments_fanfic_id_fkey");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comments_parent_comment_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("comments_user_id_fkey");
        });

        modelBuilder.Entity<ContentRating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("content_ratings_pkey");

            entity.ToTable("content_ratings");

            entity.HasIndex(e => e.Name, "content_ratings_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(32)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Fanfic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fanfics_pkey");

            entity.ToTable("fanfics");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContentRatingId).HasColumnName("content_rating_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ContentRating).WithMany(p => p.Fanfics)
                .HasForeignKey(d => d.ContentRatingId)
                .HasConstraintName("fanfics_content_rating_id_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.Fanfics)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fanfics_status_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Fanfics)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fanfics_user_id_fkey");
        });

        modelBuilder.Entity<FanficStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fanfic_statuses_pkey");

            entity.ToTable("fanfic_statuses");

            entity.HasIndex(e => e.Name, "fanfic_statuses_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(32)
                .HasColumnName("name");
        });

        modelBuilder.Entity<FanficTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fanfic_tags_pkey");

            entity.ToTable("fanfic_tags");

            entity.HasIndex(e => new { e.FanficId, e.TagId }, "uq_fanfic_tag").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FanficId).HasColumnName("fanfic_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");

            entity.HasOne(d => d.Fanfic).WithMany(p => p.FanficTags)
                .HasForeignKey(d => d.FanficId)
                .HasConstraintName("fanfic_tags_fanfic_id_fkey");

            entity.HasOne(d => d.Tag).WithMany(p => p.FanficTags)
                .HasForeignKey(d => d.TagId)
                .HasConstraintName("fanfic_tags_tag_id_fkey");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("likes_pkey");

            entity.ToTable("likes");

            entity.HasIndex(e => new { e.UserId, e.FanficId }, "uq_like").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FanficId).HasColumnName("fanfic_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Fanfic).WithMany(p => p.Likes)
                .HasForeignKey(d => d.FanficId)
                .HasConstraintName("likes_fanfic_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Likes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("likes_user_id_fkey");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tags_pkey");

            entity.ToTable("tags");

            entity.HasIndex(e => e.Name, "tags_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
