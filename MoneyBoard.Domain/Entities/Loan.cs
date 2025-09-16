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

        public DateTime GetNextDueDate()
        {
            var lastRepayment = Repayments?.Where(r => !r.IsDeleted).OrderByDescending(r => r.RepaymentDate).FirstOrDefault();
            var baseDate = lastRepayment?.RepaymentDate ?? StartDate.ToDateTime(TimeOnly.MinValue);

            return RepaymentFrequency switch
            {
                RepaymentFrequencyType.Monthly => baseDate.AddMonths(1),
                RepaymentFrequencyType.Quarterly => baseDate.AddMonths(3),
                RepaymentFrequencyType.Yearly => baseDate.AddYears(1),
                RepaymentFrequencyType.LumpSum => EndDate?.ToDateTime(TimeOnly.MinValue) ?? baseDate.AddYears(1),
                _ => baseDate.AddMonths(1)
            };
        }

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

        public void SetPrincipal(decimal principal)
        {
            if (HasRepaymentStarted)
                throw new InvalidOperationException("PRINCIPAL_LOCKED_AFTER_REPAYMENTS");

            Principal = principal;
        }

        public int GetDurationInMonths()
        {
            var endDate = EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return (endDate.Year - StartDate.Year) * 12 + (endDate.Month - StartDate.Month);
        }

        public decimal CalculateAccruedInterest(DateTime toDate)
        {
            var startDateTime = StartDate.ToDateTime(TimeOnly.MinValue);
            var toDateTime = toDate;

            if (toDateTime <= startDateTime)
                return 0;

            var timePeriod = (toDateTime - startDateTime).TotalDays / 365.0;
            if (timePeriod == 0)
                timePeriod = 1.0 / 365.0; // Accrue for at least one day on same date

            var rate = InterestRate / 100;

            decimal result = InterestType == InterestType.Compound
                ? Principal * ((decimal)Math.Pow(1 + (double)rate, timePeriod) - 1)
                : Principal * (rate * (decimal)timePeriod);

            return Math.Round(result, 2, MidpointRounding.ToEven);
        }

        public decimal CalculateTotalInterest()
        {
            int months = GetDurationInMonths();
            decimal principal = Principal;
            decimal annualRate = InterestRate / 100m;
            decimal totalInterest = 0;

            if (InterestType == InterestType.Flat)
            {
                totalInterest = principal * annualRate * (months / 12m);
            }
            else // Compound
            {
                decimal r = annualRate / 12m;
                int n = months;
                if (r > 0 && n > 0)
                {
                    decimal pow = (decimal)Math.Pow(1 + (double)r, n);
                    decimal monthlyEMI = principal * r * pow / (pow - 1);
                    decimal totalAmount = monthlyEMI * n;
                    totalInterest = totalAmount - principal;
                }
            }
            return Math.Round(totalInterest, 2, MidpointRounding.ToEven);
        }

        public decimal CalculateTotalAmount()
        {
            return Principal + CalculateTotalInterest();
        }

        /// <summary>
        /// Allocates repayment: interest first, then principal. Throws if overpayment not allowed.
        /// </summary>
        public (decimal interestPortion, decimal principalPortion, decimal overpayment)
            AllocateRepayment(decimal amount, DateTime repaymentDate, bool allowOverpayment)
        {
            var outstandingPrincipal = Principal - Repayments.Where(r => !r.IsDeleted).Sum(r => r.PrincipalComponent);

            decimal interestDue;
            var periods = CalculateNumberOfPeriods();
            if (InterestType == InterestType.Flat)
            {
                interestDue = periods > 0 ? CalculateTotalInterest() / periods : 0;
            }
            else // Compound
            {
                int periodsPerYear = RepaymentFrequency switch
                {
                    RepaymentFrequencyType.Monthly => 12,
                    RepaymentFrequencyType.Quarterly => 4,
                    RepaymentFrequencyType.Yearly => 1,
                    _ => 12
                };
                interestDue = outstandingPrincipal * (InterestRate / 100) / periodsPerYear;
            }

            var interestPortion = interestDue;
            var principalPortion = amount - interestPortion;

            var totalInterest = CalculateTotalInterest();
            var totalInterestPaid = Repayments.Where(r => !r.IsDeleted).Sum(r => r.InterestComponent);
            var outstandingInterest = Math.Max(0, totalInterest - totalInterestPaid);

            // Adjust for last payment
            if (principalPortion > outstandingPrincipal)
            {
                principalPortion = outstandingPrincipal;
                interestPortion = amount - principalPortion;
            }

            if (interestPortion > outstandingInterest)
            {
                interestPortion = outstandingInterest;
                principalPortion = amount - interestPortion;
            }

            var remaining = amount - interestPortion - principalPortion;

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
            var outstandingPrincipal = Principal - principalRepaid;

            var totalInterest = CalculateTotalInterest();
            var interestPaid = Repayments.Where(r => !r.IsDeleted).Sum(r => r.InterestComponent);
            var outstandingInterest = Math.Max(0, totalInterest - interestPaid);

            var balance = outstandingPrincipal + outstandingInterest;

            // Update status based on balance and due date
            if (balance <= 0)
            {
                Status = LoanStatus.Completed;
            }
            else if (EndDate.HasValue && asOfDate.Date > EndDate.Value.ToDateTime(TimeOnly.MinValue) && balance > 0)
            {
                Status = LoanStatus.Overdue;
            }
            else
            {
                Status = LoanStatus.Active;
            }

            return Math.Round(balance, 2, MidpointRounding.ToEven);
        }

        /// <summary>
        /// Calculate the EMI (Equated Monthly Installment) amount for the loan
        /// </summary>
        public decimal CalculateEmiAmount()
        {
            if (EndDate == null)
                return 0;

            int n = CalculateNumberOfPeriods();
            if (n <= 0)
                return 0;

            // Calculation moved to application layer.
            decimal totalAmount = 0;

            if (InterestType == InterestType.Compound)
            {
                int periodsPerYear = RepaymentFrequency switch
                {
                    RepaymentFrequencyType.Monthly => 12,
                    RepaymentFrequencyType.Quarterly => 4,
                    RepaymentFrequencyType.Yearly => 1,
                    _ => 12
                };

                decimal r = InterestRate / 100 / periodsPerYear;
                if (r <= 0)
                    return 0;

                decimal emi = Principal * r * (decimal)Math.Pow(1 + (double)r, n) / ((decimal)Math.Pow(1 + (double)r, n) - 1);
                return Math.Round(emi, 2, MidpointRounding.ToEven);
            }
            else // Flat
            {
                decimal emi = (Principal + CalculateTotalInterest()) / n; // Use Principal + TotalInterest for flat EMI
                return Math.Round(emi, 2, MidpointRounding.ToEven);
            }
        }

        /// <summary>
        /// Calculate the number of repayment periods for the loan
        /// </summary>
        private int CalculateNumberOfPeriods()
        {
            if (EndDate == null)
                return 0;

            var start = StartDate;
            var end = EndDate.Value;

            if (RepaymentFrequency == RepaymentFrequencyType.LumpSum)
                return 1;

            int months = (end.Year - start.Year) * 12 + end.Month - start.Month;
            if (end.Day < start.Day) months--;

            if (months <= 0) return 0;

            return RepaymentFrequency switch
            {
                RepaymentFrequencyType.Monthly => months,
                RepaymentFrequencyType.Quarterly => (int)Math.Ceiling(months / 3.0),
                RepaymentFrequencyType.Yearly => (int)Math.Ceiling(months / 12.0),
                _ => months
            };
        }

    }
}
