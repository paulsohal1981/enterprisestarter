using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrgManagement.Domain.Entities;

namespace OrgManagement.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.OldValues)
            .HasColumnType("text");

        builder.Property(a => a.NewValues)
            .HasColumnType("text");

        builder.Property(a => a.UserId)
            .HasMaxLength(100);

        builder.Property(a => a.UserEmail)
            .HasMaxLength(256);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(50);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.EntityId);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.OrganizationId);
    }
}
