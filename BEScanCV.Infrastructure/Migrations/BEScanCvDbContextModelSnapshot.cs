using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

[DbContext(typeof(BEScanCvDbContext))]
partial class BEScanCvDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("BEScanCV.Domain.Entities.CvFile", entity =>
        {
            entity.Property<long>("Id").ValueGeneratedOnAdd().HasColumnName("id");
            NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(entity.Property<long>("Id"));
            entity.Property<string>("AiDocumentId").HasMaxLength(255).HasColumnName("ai_document_id");
            entity.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            entity.Property<long>("FileSize").HasColumnName("file_size");
            entity.Property<string>("FileType").IsRequired().HasMaxLength(20).HasColumnName("file_type");
            entity.Property<string>("FileUrl").IsRequired().HasColumnType("text").HasColumnName("file_url");
            entity.Property<string>("OriginalFileName").IsRequired().HasMaxLength(255).HasColumnName("original_file_name");
            entity.Property<DateTime>("UpdatedAt").HasColumnName("updated_at");
            entity.Property<long>("UploadedBy").HasColumnName("uploaded_by");
            entity.HasKey("Id");
            entity.HasIndex("UploadedBy");
            entity.ToTable("cv_files", (string)null);
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.CvInfo", entity =>
        {
            entity.Property<long>("Id").ValueGeneratedOnAdd().HasColumnName("id");
            NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(entity.Property<long>("Id"));
            entity.Property<string>("Address").HasMaxLength(500).HasColumnName("address");
            entity.Property<string[]>("Certifications").IsRequired().HasColumnType("text[]").HasColumnName("certifications");
            entity.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            entity.Property<long>("CvFileId").HasColumnName("cv_file_id");
            entity.Property<DateOnly?>("DateOfBirth").HasColumnName("date_of_birth");
            entity.Property<string[]>("Educations").IsRequired().HasColumnType("text[]").HasColumnName("educations");
            entity.Property<string>("Email").IsRequired().HasMaxLength(255).HasColumnName("email");
            entity.Property<string>("FullName").IsRequired().HasMaxLength(255).HasColumnName("full_name");
            entity.Property<string>("Phone").HasMaxLength(50).HasColumnName("phone");
            entity.Property<string>("Position").HasMaxLength(255).HasColumnName("position");
            entity.Property<string>("Status").IsRequired().HasMaxLength(20).HasColumnName("status");
            entity.Property<string>("Summary").HasColumnType("text").HasColumnName("summary");
            entity.Property<DateTime>("UpdatedAt").HasColumnName("updated_at");
            entity.HasKey("Id");
            entity.HasIndex("CvFileId").IsUnique();
            entity.ToTable("cv_infos", (string)null);
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.CvSkill", entity =>
        {
            entity.Property<long>("Id").ValueGeneratedOnAdd().HasColumnName("id");
            NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(entity.Property<long>("Id"));
            entity.Property<long>("CvInfoId").HasColumnName("cv_infos_id");
            entity.Property<string>("Name").IsRequired().HasMaxLength(100).HasColumnName("name");
            entity.Property<decimal?>("YearsOfExperience").HasPrecision(4, 1).HasColumnName("years_of_experience");
            entity.HasKey("Id");
            entity.HasIndex("CvInfoId");
            entity.HasIndex("Name");
            entity.ToTable("cv_skills", (string)null);
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.RefreshToken", entity =>
        {
            entity.Property<long>("Id").ValueGeneratedOnAdd().HasColumnName("id");
            NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(entity.Property<long>("Id"));
            entity.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            entity.Property<DateTime>("ExpiresAt").HasColumnName("expires_at");
            entity.Property<DateTime?>("RevokedAt").HasColumnName("revoked_at");
            entity.Property<string>("TokenHash").IsRequired().HasMaxLength(255).HasColumnName("token_hash");
            entity.Property<long>("UserId").HasColumnName("user_id");
            entity.HasKey("Id");
            entity.HasIndex("UserId");
            entity.ToTable("refresh_tokens", (string)null);
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.User", entity =>
        {
            entity.Property<long>("Id").ValueGeneratedOnAdd().HasColumnName("id");
            NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(entity.Property<long>("Id"));
            entity.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            entity.Property<string>("Email").IsRequired().HasMaxLength(255).HasColumnName("email");
            entity.Property<string>("FullName").IsRequired().HasMaxLength(255).HasColumnName("full_name");
            entity.Property<string>("PasswordHash").IsRequired().HasMaxLength(255).HasColumnName("password_hash");
            entity.Property<string>("Role").IsRequired().HasMaxLength(20).HasColumnName("role");
            entity.Property<string>("Status").IsRequired().HasMaxLength(20).HasColumnName("status");
            entity.Property<DateTime>("UpdatedAt").HasColumnName("updated_at");
            entity.HasKey("Id");
            entity.HasAlternateKey("Email");
            entity.ToTable("users", (string)null);
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.CvFile", entity =>
        {
            entity.HasOne("BEScanCV.Domain.Entities.User", "Uploader")
                .WithMany("CvFiles")
                .HasForeignKey("UploadedBy")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.Navigation("Uploader");
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.CvInfo", entity =>
        {
            entity.HasOne("BEScanCV.Domain.Entities.CvFile", "CvFile")
                .WithOne("CvInfo")
                .HasForeignKey("BEScanCV.Domain.Entities.CvInfo", "CvFileId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.Navigation("CvFile");
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.CvSkill", entity =>
        {
            entity.HasOne("BEScanCV.Domain.Entities.CvInfo", "CvInfo")
                .WithMany("CvSkills")
                .HasForeignKey("CvInfoId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.Navigation("CvInfo");
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.RefreshToken", entity =>
        {
            entity.HasOne("BEScanCV.Domain.Entities.User", "User")
                .WithMany("RefreshTokens")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.Navigation("User");
        });

        modelBuilder.Entity("BEScanCV.Domain.Entities.CvFile", entity => entity.Navigation("CvInfo"));
        modelBuilder.Entity("BEScanCV.Domain.Entities.CvInfo", entity => entity.Navigation("CvSkills"));
        modelBuilder.Entity("BEScanCV.Domain.Entities.User", entity =>
        {
            entity.Navigation("CvFiles");
            entity.Navigation("RefreshTokens");
        });
    }
}
