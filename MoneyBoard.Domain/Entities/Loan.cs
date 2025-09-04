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
        public InterestType InterestType { get; set; } // Flat or Compound
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; }
        public LoanStatus Status { get; set; }
        public string? Notes { get; set; }

        public User? User { get; set; }
        public ICollection<Repayment> Repayments { get; set; } = new List<Repayment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public bool HasRepaymentStarted => Repayments != null && Repayments.Any(r => !r.IsDeleted);

        protected Loan()
        { }

        public Loan(Guid userId, string counterpartyName, decimal principal, decimal interestRate, InterestType type, DateOnly start,
            RepaymentFrequencyType repaymentFrequency = RepaymentFrequencyType.Monthly)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CounterpartyName = counterpartyName;
            Principal = principal;
            InterestRate = interestRate;
            InterestType = type;
            StartDate = start;
            RepaymentFrequency = repaymentFrequency;
            Status = LoanStatus.Active;
            Repayments = new List<Repayment>();
        }

        public void SetPrincipal(decimal principal) => Principal = principal;

        public decimal CalculateTotalAmount()
        {
            var principal = Principal;
            var rate = InterestRate / 100;
            var endDate = EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var startDateTime = StartDate.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDate.ToDateTime(TimeOnly.MinValue);

            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
            var effectiveEndDate = endDateTime < startDateTime ? currentDate : endDateTime;

            var timePeriod = (effectiveEndDate - startDateTime).TotalDays / 365.0;
            timePeriod = Math.Max(timePeriod, 1.0 / 365.0);

            decimal result = InterestType == InterestType.Compound
                ? principal * (decimal)Math.Pow(1 + (double)rate, timePeriod)
                : principal * (1 + rate * (decimal)timePeriod);

            return Math.Round(result, 2, MidpointRounding.ToEven);
        }

        public decimal CalculateAccruedInterest(DateTime toDate)
        {
            var startDateTime = StartDate.ToDateTime(TimeOnly.MinValue);
            var toDateTime = toDate.Date;

            if (toDateTime <= startDateTime)
                return 0;

            var timePeriod = (toDateTime - startDateTime).TotalDays / 365.0;
            var rate = InterestRate / 100;

            decimal result = InterestType == InterestType.Compound
                ? Principal * ((decimal)Math.Pow(1 + (double)rate, timePeriod) - 1)
                : Principal * (rate * (decimal)timePeriod);

            return Math.Round(result, 2, MidpointRounding.ToEven);
        }

        /// <summary>
        /// Allocates repayment: interest first, then principal. Throws if overpayment not allowed.
        /// </summary>
        public (decimal interestPortion, decimal principalPortion, decimal overpayment)
            AllocateRepayment(decimal amount, DateTime repaymentDate, bool allowOverpayment)
        {
            var accruedInterest = CalculateAccruedInterest(repaymentDate);
            var totalInterestPaid = Repayments.Where(r => !r.IsDeleted).Sum(r => r.InterestComponent);
            var outstandingInterest = Math.Max(0, accruedInterest - totalInterestPaid);

            var interestPortion = Math.Min(amount, outstandingInterest);
            var remaining = amount - interestPortion;

            var outstandingPrincipal = Principal - Repayments.Where(r => !r.IsDeleted).Sum(r => r.PrincipalComponent);
            var principalPortion = Math.Min(remaining, outstandingPrincipal);
            remaining -= principalPortion;

            if (remaining > 0 && !allowOverpayment)
                throw new InvalidOperationException("OVERPAYMENT_NOT_ALLOWED: Repayment exceeds outstanding balance.");

            return (
                Math.Round(interestPortion, 2, MidpointRounding.ToEven),
                Math.Round(principalPortion, 2, MidpointRounding.ToEven),
                Math.Round(remaining, 2, MidpointRounding.ToEven)
            );
        }

        /// <summary>
        /// Outstanding balance as of "now"
        /// </summary>
        public decimal CalculateOutstandingBalance() =>
            CalculateOutstandingBalance(DateTime.UtcNow);

        /// <summary>
        /// Outstanding balance as of a specific date (for repayments)
        /// </summary>
        public decimal CalculateOutstandingBalance(DateTime asOfDate)
        {
            var principalRepaid = Repayments.Where(r => !r.IsDeleted).Sum(r => r.PrincipalComponent);
            var accruedInterest = CalculateAccruedInterest(asOfDate);
            var interestPaid = Repayments.Where(r => !r.IsDeleted).Sum(r => r.InterestComponent);

            var outstandingPrincipal = Principal - principalRepaid;
            var outstandingInterest = Math.Max(0, accruedInterest - interestPaid);

            var balance = outstandingPrincipal + outstandingInterest;

            if (balance <= 0)
                Status = LoanStatus.Completed;

            return Math.Round(balance, 2, MidpointRounding.ToEven);
        }
    }
}
