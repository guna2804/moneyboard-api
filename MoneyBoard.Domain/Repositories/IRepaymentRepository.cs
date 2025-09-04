using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Domain.Repositories
{
    public interface IRepaymentRepository
    {
        Task<Repayment?> GetByIdAsync(Guid id);
        Task<IEnumerable<Repayment>> GetRepaymentsByLoanIdAsync(Guid loanId, int page, int pageSize, string? sortBy = null, string? filter = null);
        Task<int> GetRepaymentCountAsync(Guid loanId, string? filter = null);
        Task AddRepaymentAsync(Repayment repayment);
        Task UpdateRepaymentAsync(Repayment repayment);
        Task SoftDeleteRepaymentAsync(Guid id);
        Task<int> SaveChangesAsync();
    }
}
