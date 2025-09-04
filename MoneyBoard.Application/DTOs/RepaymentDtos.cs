using System.ComponentModel.DataAnnotations;

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
}