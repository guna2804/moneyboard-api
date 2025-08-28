using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Domain.Repositories
{
    public interface ILoanRepository
    {
        Task<Loan?> GetByIdAsync(Guid id);
        Task<IEnumerable<Loan>> GetLoansAsync(string? role, string? status, int page, int pageSize);
        Task<int> GetTotalLoansCountAsync(string? role, string? status);
        Task<Loan> CreateAsync(Loan loan);
        Task<Loan> UpdateAsync(Loan loan);
        Task SoftDeleteAsync(Guid id);
        Task<Loan> AmendAsync(Guid id, Loan amendment);
    }
}
