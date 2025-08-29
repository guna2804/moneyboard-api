using MoneyBoard.Domain.Enums;
using System;

namespace MoneyBoard.Application.DTOs
{
    public class CreateLoanDto
    {
        public Guid UserId { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Lender" or "Borrower"
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; }
        public CompoundingFrequencyType CompoundingFrequency { get; set; } = CompoundingFrequencyType.Monthly;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; }
        public int Version { get; set; } = 1;
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
        public InterestType InterestType { get; set; }
        public CompoundingFrequencyType CompoundingFrequency { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public RepaymentFrequencyType RepaymentFrequency { get; set; }
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; }
        public LoanStatus Status { get; set; }
        public int Version { get; set; }
        public string? Notes { get; set; } // optional notes about the loan
    }

    public class UpdateLoanDto
    {
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal Principal { get; set; } // Only editable before first repayment
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; }
        public CompoundingFrequencyType CompoundingFrequency { get; set; } = CompoundingFrequencyType.Monthly;
        public DateOnly StartDate { get; set; } // Only editable before first repayment
        public DateOnly? EndDate { get; set; }
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; }
        public string? Notes { get; set; }
    }

    public class AmendLoanDto
    {
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; }
        public CompoundingFrequencyType CompoundingFrequency { get; set; } = CompoundingFrequencyType.Monthly;
        public DateOnly? EndDate { get; set; }
        public RepaymentFrequencyType RepaymentFrequency { get; set; } = RepaymentFrequencyType.Monthly;
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; }
        public string? Notes { get; set; }
    }

    public class PagedLoanResponseDto
    {
        public IEnumerable<LoanDetailsDto> Loans { get; set; } = new List<LoanDetailsDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}