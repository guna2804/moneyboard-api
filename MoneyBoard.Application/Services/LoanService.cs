using AutoMapper;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Application.Services
{
    public class LoanService(ILoanRepository loanRepository, IMapper mapper) : ILoanService
    {
        public async Task<LoanDetailsDto> GetLoanByIdAsync(Guid id, Guid userId)
        {
            var loan = await loanRepository.GetByIdAsync(id);
            if (loan == null || loan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            return mapper.Map<LoanDetailsDto>(loan);
        }

        public async Task<PagedLoanResponseDto> GetLoansAsync(string? role, string? status, int page, int pageSize, Guid userId)
        {
            var loans = await loanRepository.GetLoansAsync(role, status, page, pageSize);
            var totalCount = await loanRepository.GetTotalLoansCountAsync(role, status);

            // Filter by userId since repository doesn't filter by user
            var userLoans = loans.Where(l => l.UserId == userId);

            var loanDtos = mapper.Map<IEnumerable<LoanDetailsDto>>(userLoans);

            return new PagedLoanResponseDto
            {
                Loans = loanDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<LoanDetailsDto> CreateLoanAsync(CreateLoanDto dto, Guid userId)
        {
            var loan = mapper.Map<Loan>(dto);
            loan.UserId = userId;

            var createdLoan = await loanRepository.CreateAsync(loan);
            return mapper.Map<LoanDetailsDto>(createdLoan);
        }

        public async Task<LoanDetailsDto> UpdateLoanAsync(Guid id, UpdateLoanDto dto, Guid userId)
        {
            var existingLoan = await loanRepository.GetByIdAsync(id);
            if (existingLoan == null || existingLoan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            mapper.Map(dto, existingLoan);
            var updatedLoan = await loanRepository.UpdateAsync(existingLoan);
            return mapper.Map<LoanDetailsDto>(updatedLoan);
        }

        public async Task DeleteLoanAsync(Guid id, Guid userId)
        {
            var existingLoan = await loanRepository.GetByIdAsync(id);
            if (existingLoan == null || existingLoan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            await loanRepository.SoftDeleteAsync(id);
        }

        public async Task<LoanDetailsDto> AmendLoanAsync(Guid id, UpdateLoanDto dto, Guid userId)
        {
            var existingLoan = await loanRepository.GetByIdAsync(id);
            if (existingLoan == null || existingLoan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            // Create a new loan based on the existing one with amendments
            var amendedLoan = new Loan(
                existingLoan.UserId,
                dto.CounterpartyName ?? existingLoan.CounterpartyName,
                existingLoan.Principal,
                dto.InterestRate,
                existingLoan.InterestType,
                existingLoan.StartDate
            )
            {
                CompoundingFrequency = existingLoan.CompoundingFrequency,
                EndDate = existingLoan.EndDate,
                RepaymentFrequency = dto.RepaymentFrequency ?? existingLoan.RepaymentFrequency,
                AllowOverpayment = dto.AllowOverpayment,
                Currency = existingLoan.Currency,
                Status = existingLoan.Status
            };

            var createdAmendment = await loanRepository.AmendAsync(id, amendedLoan);
            return mapper.Map<LoanDetailsDto>(createdAmendment);
        }
    }
}
