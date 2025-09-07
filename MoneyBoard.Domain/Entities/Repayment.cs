using MoneyBoard.Domain.Common;
using MoneyBoard.Domain.Enums;

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
        public RepaymentStatus Status { get; private set; }

        protected Repayment()
        { } // EF

        public Repayment(Guid loanId, decimal amount, DateTime repaymentDate,
                          decimal interestComponent, decimal principalComponent, DateTime nextDueDate, string? notes = null)
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
            Status = repaymentDate.Date < nextDueDate.Date ? RepaymentStatus.Early :
                     repaymentDate.Date == nextDueDate.Date ? RepaymentStatus.OnTime :
                     RepaymentStatus.Late;
            SetCreated();
        }

        public void Update(decimal amount, DateTime repaymentDate, string? notes,
                            decimal interestComponent, decimal principalComponent, DateTime nextDueDate)
        {
            if (amount <= 0)
                throw new ArgumentException("Repayment amount must be greater than 0.", nameof(amount));

            Amount = Math.Round(amount, 2, MidpointRounding.ToEven);
            RepaymentDate = DateTime.SpecifyKind(repaymentDate, DateTimeKind.Utc);
            Notes = notes;
            InterestComponent = Math.Round(interestComponent, 2, MidpointRounding.ToEven);
            PrincipalComponent = Math.Round(principalComponent, 2, MidpointRounding.ToEven);
            Status = repaymentDate.Date < nextDueDate.Date ? RepaymentStatus.Early :
                     repaymentDate.Date == nextDueDate.Date ? RepaymentStatus.OnTime :
                     RepaymentStatus.Late;
            SetUpdated();
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            SetUpdated();
        }
    }
}
