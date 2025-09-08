namespace MoneyBoard.Application.DTOs
{
    public class DashboardSummaryDto
    {
        public decimal TotalLent { get; set; }
        public decimal LentChangePercent { get; set; }
        public decimal TotalBorrowed { get; set; }
        public decimal BorrowedChangePercent { get; set; }
        public decimal InterestEarned { get; set; }
        public decimal InterestChangePercent { get; set; }
    }

    public class RecentTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty; // ISO 8601
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // active, overdue, completed
        public string Direction { get; set; } = string.Empty; // in, out
    }

    public class RecentTransactionsResponseDto
    {
        public List<RecentTransactionDto> Transactions { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

    public class UpcomingPaymentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty; // ISO 8601
        public decimal Amount { get; set; }
        public string Direction { get; set; } = string.Empty; // in, out
    }

    public class UpcomingPaymentsResponseDto
    {
        public List<UpcomingPaymentDto> UpcomingPayments { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

    public class MonthlyRepaymentDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class LoanStatusDistributionDto
    {
        public int Active { get; set; }
        public int Closed { get; set; }
        public int Overdue { get; set; }
    }

    public class AlertDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // overdue, upcoming
        public string Message { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }

    public class PaginationDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public bool HasNext { get; set; }
    }
}