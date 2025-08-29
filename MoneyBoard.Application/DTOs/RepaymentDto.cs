using System;

namespace MoneyBoard.Application.DTOs
{
    public class RepaymentDto
    {
        public Guid Id { get; set; }
        public Guid LoanId { get; set; }
        public decimal Amount { get; set; }
        public DateTime RepaymentDate { get; set; }
        public string Allocation { get; set; } = string.Empty;
        public string AllocationDetails { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
