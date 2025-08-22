using MoneyBoard.Domain.Enums;
using System;

namespace MoneyBoard.Application.DTOs
{
    public class CreateLoanDto
    {
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Lender" or "Borrower"
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; }
        public string CompoundingFrequency { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public string RepaymentFrequency { get; set; } = "Monthly";
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; }
    }

    public class LoanDetailsDto
    {
        public Guid Id { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; }
        public string CompoundingFrequency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RepaymentFrequency { get; set; } = string.Empty;
        public bool AllowOverpayment { get; set; }
        public CurrencyType Currency { get; set; }
        public LoanStatus Status { get; set; }
    }

    public class UpdateLoanDto
    {
        public string CounterpartyName { get; set; } = string.Empty;
        public decimal InterestRate { get; set; }
        public string RepaymentFrequency { get; set; } = string.Empty;
        public bool AllowOverpayment { get; set; }
    }
}