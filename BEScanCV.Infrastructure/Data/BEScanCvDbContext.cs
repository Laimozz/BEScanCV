using BEScanCV.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace BEScanCV.Infrastructure.Data;

public sealed class BEScanCvDbContext(DbContextOptions<BEScanCvDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CvFile> CvFiles => Set<CvFile>();
    public DbSet<CvInfo> CvInfos => Set<CvInfo>();
    public DbSet<CvSkill> CvSkills => Set<CvSkill>();
    public DbSet<CvCertification> CvCertifications => Set<CvCertification>();
    public DbSet<CvWorkExperience> CvWorkExperiences => Set<CvWorkExperience>();
    public DbSet<CvUploadBatch> CvUploadBatches => Set<CvUploadBatch>();
    public DbSet<CvUploadBatchItem> CvUploadBatchItems => Set<CvUploadBatchItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Value converter cho JsonDocument — chỉ cần thiết khi dùng InMemory provider.
        // PostgreSQL (Npgsql) hỗ trợ JsonDocument native nên không cần convert.
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        var jsonDocConverter = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => v == null ? null : JsonDocument.Parse(v));

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
            entity.HasIndex(e => e.AiDocumentId);
        });

        modelBuilder.Entity<CvInfo>(entity =>
        {
            entity.ToTable("cv_infos", table =>
            {
                table.HasCheckConstraint(
                    "cv_infos_tag_check",
                    "tag IN ('new', 'contacted', 'in-process', 'rejected', 'hired')");
                table.HasCheckConstraint(
                    "cv_infos_work_type_check",
                    "work_type IS NULL OR work_type IN ('remote', 'in-house', 'onsite')");
            });
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
            entity.Property(e => e.Educations).HasColumnName("educations").HasColumnType("jsonb")
                  .HasConversion(isInMemory ? jsonDocConverter : null);
            entity.Property(e => e.RawText).HasColumnName("raw_text").HasColumnType("text");
            entity.Property(e => e.ProfileData).HasColumnName("profile_data").HasColumnType("jsonb")
                  .HasConversion(isInMemory ? jsonDocConverter : null);
            entity.Property(e => e.QualityScore).HasColumnName("quality_score");
            entity.Property(e => e.QualityReason).HasColumnName("quality_reason").HasColumnType("text");
            entity.Property(e => e.QualityDetails).HasColumnName("quality_details").HasColumnType("jsonb")
                  .HasConversion(isInMemory ? jsonDocConverter : null);
            entity.Property(e => e.IsMarked).HasColumnName("is_marked").HasDefaultValue(false);
            entity.Property(e => e.Tag).HasColumnName("tag").HasMaxLength(20).HasDefaultValue("new").IsRequired();
            entity.Property(e => e.WorkType).HasColumnName("work_type").HasMaxLength(20);
            entity.Property(e => e.Note).HasColumnName("note").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
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

        modelBuilder.Entity<CvCertification>(entity =>
        {
            entity.ToTable("cv_certification");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.CvInfoId).HasColumnName("cv_info_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.HasOne(e => e.CvInfo)
                .WithMany(e => e.CvCertifications)
                .HasForeignKey(e => e.CvInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CvInfoId);
        });

        modelBuilder.Entity<CvWorkExperience>(entity =>
        {
            entity.ToTable("work_experience");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.CvInfoId).HasColumnName("cv_info_id");
            entity.Property(e => e.Company).HasColumnName("company").HasMaxLength(255);
            entity.Property(e => e.Position).HasColumnName("position").HasMaxLength(255);
            entity.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(255);
            entity.Property(e => e.Responsibility).HasColumnName("responsibility").HasColumnType("text");
            entity.HasOne(e => e.CvInfo)
                .WithMany(e => e.WorkExperiences)
                .HasForeignKey(e => e.CvInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CvInfoId);
        });

        modelBuilder.Entity<CvUploadBatch>(entity =>
        {
            entity.ToTable("cv_upload_batches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(64);
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TotalFiles).HasColumnName("total_files");
            entity.Property(e => e.CompletedFiles).HasColumnName("completed_files");
            entity.Property(e => e.FailedFiles).HasColumnName("failed_files");
            entity.Property(e => e.CancelledFiles).HasColumnName("cancelled_files");
            entity.Property(e => e.ProcessingFiles).HasColumnName("processing_files");
            entity.Property(e => e.PendingFiles).HasColumnName("pending_files");
            entity.Property(e => e.RequestIds).HasColumnName("request_ids").HasColumnType("text").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Uploader).WithMany(e => e.CvUploadBatches).HasForeignKey(e => e.UploadedBy);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UploadedBy);
        });

        modelBuilder.Entity<CvUploadBatchItem>(entity =>
        {
            entity.ToTable("batch_upload_items", table =>
                table.HasCheckConstraint(
                    "batch_upload_items_status_check",
                    "status IN ('QUEUE', 'PROCESSING', 'COMPLETED', 'FAILED')"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.CvUploadBatchId).HasColumnName("cv_upload_batch_id").HasMaxLength(64);
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("QUEUE").IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.CvUploadBatch)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.CvUploadBatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CvUploadBatchId);
            entity.HasIndex(e => e.Status);
        });
    }
}
