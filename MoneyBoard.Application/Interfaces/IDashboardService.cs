using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default);
        Task<RecentTransactionsResponseDto> GetRecentTransactionsAsync(Guid userId, int limit = 5, int page = 1, CancellationToken ct = default);
        Task<UpcomingPaymentsResponseDto> GetUpcomingPaymentsAsync(Guid userId, int limit = 5, int page = 1, CancellationToken ct = default);
        Task<List<MonthlyRepaymentDto>> GetMonthlyRepaymentsAsync(Guid userId, int year, CancellationToken ct = default);
        Task<LoanStatusDistributionDto> GetLoanStatusDistributionAsync(Guid userId, CancellationToken ct = default);
        Task<List<AlertDto>> GetAlertsAsync(Guid userId, CancellationToken ct = default);
    }
}