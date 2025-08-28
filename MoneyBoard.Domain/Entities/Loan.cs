using MoneyBoard.Domain.Common;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Domain.Entities
{
    public class Loan : BaseEntity
    {
        public Guid UserId { get; set; }
        public string CounterpartyName { get; set; }
        public string Role { get; set; } // "Lender" or "Borrower"
        public decimal Principal { get; private set; }
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; } // e.g., enum Flat, Compound
        public string CompoundingFrequency { get; set; } // "Monthly"
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RepaymentFrequency { get; set; }
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; } // e.g., enum USD, EUR, INR...
        public LoanStatus Status { get; set; } // e.g., Active, Overdue, Completed
        public User User { get; set; }
        public ICollection<Repayment> Repayments { get; set; }
        public ICollection<Notification> Notifications { get; set; }

        protected Loan()
        { }

        public Loan(Guid userId, string counterpartyName, decimal principal, decimal interestRate, InterestType type, DateTime start)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CounterpartyName = counterpartyName;
            Principal = principal;
            InterestRate = interestRate;
            InterestType = type;
            StartDate = start;
            Status = LoanStatus.Active;
            Repayments = new List<Repayment>();
        }
    }
}