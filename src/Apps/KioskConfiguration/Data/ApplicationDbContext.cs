using Microsoft.EntityFrameworkCore;
using Platform.Apps.KioskConfiguration.Models;

namespace Platform.Apps.KioskConfiguration.Data
{
    /// <summary>
    /// Database context per KioskConfiguration
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ConfigurationTemplateEntity> ConfigurationTemplates { get; set; }
        public DbSet<KioskConfigurationEntity> KioskConfigurations { get; set; }
        public DbSet<ConfigurationFieldValue> ConfigurationFieldValues { get; set; }
        public DbSet<ConfigurationAttachment> ConfigurationAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ConfigurationTemplateEntity
            modelBuilder.Entity<ConfigurationTemplateEntity>(entity =>
            {
                entity.ToTable("ConfigurationTemplates");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TemplateId).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.JsonContent).HasColumnType("nvarchar(max)");
            });

            // KioskConfigurationEntity
            modelBuilder.Entity<KioskConfigurationEntity>(entity =>
            {
                entity.ToTable("KioskConfigurations");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SerialNumber);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.Template)
                    .WithMany(t => t.Configurations)
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ConfigurationFieldValue
            modelBuilder.Entity<ConfigurationFieldValue>(entity =>
            {
                entity.ToTable("ConfigurationFieldValues");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ConfigurationId, e.FieldPath }).IsUnique();

                entity.HasOne(e => e.Configuration)
                    .WithMany(c => c.FieldValues)
                    .HasForeignKey(e => e.ConfigurationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.FieldValue).HasColumnType("nvarchar(max)");
            });

            // ConfigurationAttachment
            modelBuilder.Entity<ConfigurationAttachment>(entity =>
            {
                entity.ToTable("ConfigurationAttachments");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ConfigurationId);

                entity.HasOne(e => e.Configuration)
                    .WithMany(c => c.Attachments)
                    .HasForeignKey(e => e.ConfigurationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
