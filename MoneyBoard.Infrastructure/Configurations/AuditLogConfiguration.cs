using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Infrastructure.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.Property(a => a.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.EntityId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.Action)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(a => a.ChangedBy)
                .IsRequired();

            builder.Property(a => a.Details)
                .HasMaxLength(2000)
                .IsRequired();

            // Create indexes for better query performance
            builder.HasIndex(a => a.EntityType);
            builder.HasIndex(a => a.ChangedBy);
            builder.HasIndex(a => new { a.EntityType, a.CreatedAt });
            builder.HasIndex(a => a.CreatedAt);
        }
    }
}
