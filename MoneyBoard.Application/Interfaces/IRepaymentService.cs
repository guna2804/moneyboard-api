using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Interfaces
{
    public interface IRepaymentService
    {
        Task<RepaymentResponseDto> CreateRepaymentAsync(Guid loanId, CreateRepaymentRequestDto request, Guid userId);
        Task<RepaymentResponseDto> UpdateRepaymentAsync(Guid loanId, Guid repaymentId, UpdateRepaymentRequestDto request, Guid userId);
        Task<PagedRepaymentResponseDto> GetRepaymentsAsync(Guid loanId, int page, int pageSize, string? sortBy, string? filter, Guid userId);
        Task DeleteRepaymentAsync(Guid loanId, Guid repaymentId, Guid userId);
    }
}
