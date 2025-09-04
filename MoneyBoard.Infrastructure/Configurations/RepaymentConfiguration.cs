using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Infrastructure.Configurations
{
    public class RepaymentConfiguration : IEntityTypeConfiguration<Repayment>
    {
        public void Configure(EntityTypeBuilder<Repayment> builder)
        {
            builder.ToTable("repayments");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.LoanId)
                .IsRequired();

            builder.Property(r => r.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(r => r.InterestComponent)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(r => r.PrincipalComponent)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(r => r.RepaymentDate)
                .IsRequired();

            builder.Property(r => r.Notes)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(r => r.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(r => r.Loan)
                .WithMany(l => l.Repayments)
                .HasForeignKey(r => r.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => r.LoanId);
            builder.HasIndex(r => r.RepaymentDate);
            builder.HasIndex(r => new { r.LoanId, r.IsDeleted });
        }
    }
}