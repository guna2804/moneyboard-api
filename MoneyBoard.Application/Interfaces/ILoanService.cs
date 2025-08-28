using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Interfaces
{
    public interface ILoanService
    {
        Task<LoanDetailsDto> GetLoanByIdAsync(Guid id, Guid userId);
        Task<PagedLoanResponseDto> GetLoansAsync(string? role, string? status, int page, int pageSize, Guid userId);
        Task<LoanDetailsDto> CreateLoanAsync(CreateLoanDto dto, Guid userId);
        Task<LoanDetailsDto> UpdateLoanAsync(Guid id, UpdateLoanDto dto, Guid userId);
        Task DeleteLoanAsync(Guid id, Guid userId);
        Task<LoanDetailsDto> AmendLoanAsync(Guid id, UpdateLoanDto dto, Guid userId);
    }
}
