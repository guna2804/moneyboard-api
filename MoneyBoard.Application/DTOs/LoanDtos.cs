using MoneyBoard.Domain.Enums;
using System.Text.Json.Serialization;

namespace MoneyBoard.Application.DTOs
{
    public class CreateLoanDto
    {
        public Guid UserId { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Lender" or "Borrower"
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InterestType InterestType { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CurrencyType Currency { get; set; }
        public string? Notes { get; set; } // optional notes about the loan
    }

    public class LoanDetailsDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InterestType InterestType { get; set; }
 
        //public CompoundingFrequencyType CompoundingFrequency { get; set; }
        public DateOnly StartDate { get; set; }
 
        public DateOnly? EndDate { get; set; }
 
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RepaymentFrequencyType RepaymentFrequency { get; set; }
        public bool AllowOverpayment { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CurrencyType Currency { get; set; }
 
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LoanStatus Status { get; set; }
        public string? Notes { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal MonthlyEMI { get; set; }

        // Flag to indicate if loan has started repayments (affects editability)
        public bool HasRepaymentStarted { get; set; }

        // Additional fields for enhanced API
        public decimal TotalPrincipalRepaid { get; set; }
        public decimal TotalInterestPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    public class UpdateLoanDto
    {
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal Principal { get; set; } // Only editable before first repayment
        public decimal InterestRate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InterestType InterestType { get; set; }
        public DateOnly StartDate { get; set; } // Only editable before first repayment
        public DateOnly? EndDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CurrencyType Currency { get; set; }
        public string? Notes { get; set; }
    }

    public class AmendLoanDto
    {
        public decimal InterestRate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InterestType InterestType { get; set; }
        public DateOnly? EndDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CurrencyType Currency { get; set; }
        public string? Notes { get; set; }
    }

    public class OutstandingLoanDto
    {
        public Guid LoanId { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal InterestRate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LoanStatus Status { get; set; }
        public bool AllowOverpayment { get; set; }
        public DateOnly? NextDueDate { get; set; }
        public decimal EmiAmount { get; set; }
        public string? BorrowerName { get; set; }
        public string? LenderName { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class PagedLoanResponseDto
    {
        public IEnumerable<LoanDetailsDto> Loans { get; set; } = new List<LoanDetailsDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class OutstandingLoansResponseDto
    {
        public IEnumerable<OutstandingLoanDto> Loans { get; set; } = new List<OutstandingLoanDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class LoanWithRepaymentHistoryDto : LoanDetailsDto
    {
        public IEnumerable<RepaymentHistoryDto> RepaymentHistory { get; set; } = new List<RepaymentHistoryDto>();
    }

    public class RepaymentHistoryDto
    {
        public Guid RepaymentId { get; set; }
        public DateTime RepaymentDate { get; set; }
        public decimal PrincipalComponent { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RepaymentStatus Status { get; set; }
    }
}
