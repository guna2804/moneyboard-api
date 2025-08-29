using MoneyBoard.Domain.Common;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Domain.Entities
{
    public class Loan : BaseEntity
    {
        public Guid UserId { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Lender" or "Borrower"
        public decimal Principal { get; private set; }
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; } // e.g., enum Flat, Compound
        public CompoundingFrequencyType CompoundingFrequency { get; set; } = CompoundingFrequencyType.Monthly;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; } // e.g., enum USD, EUR, INR...
        public LoanStatus Status { get; set; } // e.g., Active, Overdue, Completed
        public string? Notes { get; set; }
        public User? User { get; set; }
        public ICollection<Repayment> Repayments { get; set; } = new List<Repayment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        protected Loan()
        { }

        public Loan(Guid userId, string counterpartyName, decimal principal, decimal interestRate, InterestType type, DateOnly start,
            CompoundingFrequencyType compoundingFrequency = CompoundingFrequencyType.Monthly,
            RepaymentFrequencyType repaymentFrequency = RepaymentFrequencyType.Monthly)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CounterpartyName = counterpartyName;
            Principal = principal;
            InterestRate = interestRate;
            InterestType = type;
            StartDate = start;
            CompoundingFrequency = compoundingFrequency;
            RepaymentFrequency = repaymentFrequency;
            Status = LoanStatus.Active;
            Repayments = new List<Repayment>();
        }
    }
}