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
        public string CompoundingFrequency { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RepaymentFrequency { get; set; } = "Monthly";
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; } // e.g., enum USD, EUR, INR...
        public LoanStatus Status { get; set; } // e.g., Active, Overdue, Completed
        public int Version { get; set; } = 1; // Version for amendments
        public User? User { get; set; }
        public ICollection<Repayment> Repayments { get; set; } = new List<Repayment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

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
