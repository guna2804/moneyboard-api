using MoneyBoard.Domain.Common;

namespace MoneyBoard.Domain.Entities
{
    public class Repayment : BaseEntity
    {
        public Guid LoanId { get; set; }
        public Loan? Loan { get; set; }
        public decimal Amount { get; set; }
        public DateTime RepaymentDate { get; set; }
        public string Allocation { get; set; } = string.Empty;
        public string AllocationDetails { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        protected Repayment()
        { }

        public Repayment(Guid loanId, decimal amount, DateTime date)
        {
            Id = Guid.NewGuid();
            LoanId = loanId;
            Amount = amount;
            RepaymentDate = date; // Now this will work
        }
    }
}
