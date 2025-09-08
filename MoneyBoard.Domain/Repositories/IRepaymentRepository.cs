using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Domain.Repositories
{
    public class RepaymentSummaryData
    {
        public decimal TotalPayments { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPrincipal { get; set; }
    }

    public interface IRepaymentRepository
    {
        Task<Repayment?> GetByIdAsync(Guid id);
        Task<IEnumerable<Repayment>> GetRepaymentsByLoanIdAsync(Guid loanId, int page, int pageSize, string? sortBy = null, string? filter = null);
        Task<int> GetRepaymentCountAsync(Guid loanId, string? filter = null);
        Task<IEnumerable<Repayment>> GetRepaymentsByUserRoleAsync(Guid userId, string userRole);
        Task AddRepaymentAsync(Repayment repayment);
        Task UpdateRepaymentAsync(Repayment repayment);
        Task SoftDeleteRepaymentAsync(Guid id);
        Task<int> SaveChangesAsync();
        Task<IEnumerable<Repayment>> GetRecentRepaymentsByUserAsync(Guid userId, int limit, int offset);
        Task<int> GetRecentRepaymentsCountByUserAsync(Guid userId);
        Task<bool> HasRepaymentInPeriodAsync(Guid loanId, DateTime repaymentDate, RepaymentFrequencyType frequency, Guid? excludeRepaymentId = null);
        Task<RepaymentSummaryData> GetRepaymentSummaryDataAsync(Guid userId, string role);
    }
}
