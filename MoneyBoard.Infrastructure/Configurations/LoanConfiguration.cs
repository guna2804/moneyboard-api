using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Infrastructure.Configurations
{
    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.Property(l => l.InterestType)
             .HasConversion<string>()
             .HasColumnType("text");

            builder.Property(l => l.Status)
                .HasConversion<string>()
                .HasColumnType("text");

            builder.Property(l => l.Currency)
                .HasConversion<string>()
                .HasColumnType("text");

            builder.Property(l => l.RepaymentFrequency)
                .HasConversion<string>()
                .HasColumnType("text");
        }
    }
}
