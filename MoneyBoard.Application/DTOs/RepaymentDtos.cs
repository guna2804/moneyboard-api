using System.ComponentModel.DataAnnotations;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Application.DTOs
{
    public class CreateRepaymentRequestDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime RepaymentDate { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdateRepaymentRequestDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime RepaymentDate { get; set; }

        public string? Notes { get; set; }
    }

    public class RepaymentDto
    {
        public Guid Id { get; set; }
        public Guid LoanId { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal PrincipalComponent { get; set; }
        public DateTime RepaymentDate { get; set; }
        public string? Notes { get; set; }
        public RepaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RepaymentResponseDto : RepaymentDto
    {
        public decimal NewBalance { get; set; }
    }

    public class PagedRepaymentResponseDto
    {
        public IEnumerable<RepaymentDto> Repayments { get; set; } = new List<RepaymentDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class RepaymentSummaryDto
    {
        public decimal TotalPayments { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPrincipal { get; set; }
        public RepaymentBreakdownDto? LendingBreakdown { get; set; }
        public RepaymentBreakdownDto? BorrowingBreakdown { get; set; }
    }

    public class RepaymentResult
    {
        public bool IsSuccess { get; private set; }
        public RepaymentResponseDto? Data { get; private set; }
        public string? ErrorMessage { get; private set; }

        private RepaymentResult() { }

        public static RepaymentResult Success(RepaymentResponseDto data)
        {
            return new RepaymentResult
            {
                IsSuccess = true,
                Data = data,
                ErrorMessage = null
            };
        }

        public static RepaymentResult Error(string message)
        {
            return new RepaymentResult
            {
                IsSuccess = false,
                Data = null,
                ErrorMessage = message
            };
        }
    }

    public class RepaymentBreakdownDto
    {
        public decimal TotalPayments { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPrincipal { get; set; }
    }
}