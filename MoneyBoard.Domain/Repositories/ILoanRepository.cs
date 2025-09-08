using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Domain.Repositories
{
    public interface ILoanRepository
    {
        Task<Loan?> GetByIdAsync(Guid id);

        Task<IEnumerable<Loan>> GetLoansAsync(string? role, string? status, int page, int pageSize, Guid userId);

        Task<int> GetTotalLoansCountAsync(string? role, string? status, Guid userId);

        Task<Loan> CreateAsync(Loan loan);

        Task<Loan> UpdateAsync(Loan loan);

        Task SoftDeleteAsync(Guid id);

        Task<Loan> AmendAsync(Guid id, Loan amendment);
        Task<IEnumerable<Loan>> GetActiveLoansAsync(Guid userId);
        Task<decimal> GetTotalLentAsync(Guid userId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalBorrowedAsync(Guid userId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalInterestEarnedAsync(Guid userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Loan>> GetLoansWithUpcomingPaymentsAsync(Guid userId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, int>> GetLoanStatusDistributionAsync(Guid userId);
        Task<Dictionary<string, decimal>> GetMonthlyRepaymentTotalsAsync(Guid userId, int year);
        Task<IEnumerable<Loan>> GetOverdueLoansAsync(Guid userId);
        Task<IEnumerable<Loan>> GetLoansWithUpcomingDueDatesAsync(Guid userId, DateTime fromDate, DateTime toDate);
    }
}
