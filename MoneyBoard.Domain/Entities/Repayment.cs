using MoneyBoard.Domain.Common;

namespace MoneyBoard.Domain.Entities
{
    public class Repayment : BaseEntity
    {
        public Guid LoanId { get; private set; }
        public Loan? Loan { get; private set; } // EF will set this

        public decimal Amount { get; private set; }
        public decimal InterestComponent { get; private set; }
        public decimal PrincipalComponent { get; private set; }

        public DateTime RepaymentDate { get; private set; }
        public string? Notes { get; private set; }

        protected Repayment()
        { } // EF

        public Repayment(Guid loanId, decimal amount, DateTime repaymentDate,
                         decimal interestComponent, decimal principalComponent, string? notes = null)
        {
            if (amount <= 0)
                throw new ArgumentException("Repayment amount must be greater than 0.", nameof(amount));

            Id = Guid.NewGuid();
            LoanId = loanId;
            Amount = Math.Round(amount, 2, MidpointRounding.ToEven);
            RepaymentDate = DateTime.SpecifyKind(repaymentDate, DateTimeKind.Utc);
            InterestComponent = Math.Round(interestComponent, 2, MidpointRounding.ToEven);
            PrincipalComponent = Math.Round(principalComponent, 2, MidpointRounding.ToEven);
            Notes = notes;
            SetCreated();
        }

        public void Update(decimal amount, DateTime repaymentDate, string? notes,
                           decimal interestComponent, decimal principalComponent)
        {
            if (amount <= 0)
                throw new ArgumentException("Repayment amount must be greater than 0.", nameof(amount));

            Amount = Math.Round(amount, 2, MidpointRounding.ToEven);
            RepaymentDate = DateTime.SpecifyKind(repaymentDate, DateTimeKind.Utc);
            Notes = notes;
            InterestComponent = Math.Round(interestComponent, 2, MidpointRounding.ToEven);
            PrincipalComponent = Math.Round(principalComponent, 2, MidpointRounding.ToEven);
            SetUpdated();
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            SetUpdated();
        }
    }
}
