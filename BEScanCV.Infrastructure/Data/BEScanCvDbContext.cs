using BEScanCV.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Data;

public sealed class BEScanCvDbContext(DbContextOptions<BEScanCvDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CvFile> CvFiles => Set<CvFile>();
    public DbSet<CvInfo> CvInfos => Set<CvInfo>();
    public DbSet<CvSkill> CvSkills => Set<CvSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasAlternateKey(e => e.Email);

        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.User).WithMany(e => e.RefreshTokens).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<CvFile>(entity =>
        {
            entity.ToTable("cv_files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.FileUrl).HasColumnName("file_url").HasColumnType("text").IsRequired();
            entity.Property(e => e.FileType).HasColumnName("file_type").HasMaxLength(20);
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.AiDocumentId).HasColumnName("ai_document_id").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Uploader).WithMany(e => e.CvFiles).HasForeignKey(e => e.UploadedBy);
            entity.HasOne(e => e.CvInfo).WithOne(e => e.CvFile).HasForeignKey<CvInfo>(e => e.CvFileId);
        });

        modelBuilder.Entity<CvInfo>(entity =>
        {
            entity.ToTable("cv_infos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.CvFileId).HasColumnName("cv_file_id");
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(e => e.Position).HasColumnName("position").HasMaxLength(255);
            entity.Property(e => e.TotalExperienceYears).HasColumnName("total_experience_years");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
            entity.Property(e => e.Summary).HasColumnName("summary").HasColumnType("text");
            entity.Property(e => e.Educations).HasColumnName("educations").HasColumnType("jsonb");
            entity.Property(e => e.RawText).HasColumnName("raw_text").HasColumnType("text");
            entity.Property(e => e.ProfileData).HasColumnName("profile_data").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<CvSkill>(entity =>
        {
            entity.ToTable("cv_skills");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.CvInfoId).HasColumnName("cv_infos_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.HasOne(e => e.CvInfo).WithMany(e => e.CvSkills).HasForeignKey(e => e.CvInfoId);
            entity.HasIndex(e => e.CvInfoId);
            entity.HasIndex(e => e.Name);
        });
    }
}
