using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrgManagement.Domain.Entities;

namespace OrgManagement.Infrastructure.Data.Configurations;

public class SubOrganizationConfiguration : IEntityTypeConfiguration<SubOrganization>
{
    public void Configure(EntityTypeBuilder<SubOrganization> builder)
    {
        builder.ToTable("SubOrganizations");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Code)
            .HasMaxLength(50);

        builder.Property(s => s.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Level)
            .IsRequired();

        // Index for efficient hierarchy queries using materialized path
        builder.HasIndex(s => s.Path);

        builder.HasIndex(s => s.Level);

        builder.HasIndex(s => s.Code)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(s => new { s.OrganizationId, s.Name });

        // Self-referencing relationship for hierarchy
        builder.HasOne(s => s.ParentSubOrganization)
            .WithMany(s => s.ChildSubOrganizations)
            .HasForeignKey(s => s.ParentSubOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Users relationship
        builder.HasMany(s => s.Users)
            .WithOne(u => u.SubOrganization)
            .HasForeignKey(u => u.SubOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
