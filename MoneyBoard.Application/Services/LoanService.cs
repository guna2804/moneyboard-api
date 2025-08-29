using AutoMapper;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository loanRepository;
        private readonly IMapper mapper;

        public LoanService(ILoanRepository loanRepository, IMapper mapper)
        {
            this.loanRepository = loanRepository;
            this.mapper = mapper;
        }

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
            loan.StartDate = dto.StartDate;
            loan.EndDate = dto.EndDate;

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

            bool hasRepayments = existingLoan.Repayments != null && existingLoan.Repayments.Any();

            if (hasRepayments)
            {
                existingLoan.CounterpartyName = dto.CounterpartyName;
                existingLoan.Role = dto.Role;
                existingLoan.EndDate = dto.EndDate;
                existingLoan.RepaymentFrequency = dto.RepaymentFrequency;
                existingLoan.AllowOverpayment = dto.AllowOverpayment;
                existingLoan.Currency = dto.Currency;
                existingLoan.Notes = dto.Notes;
            }
            else
            {
                mapper.Map(dto, existingLoan);
                existingLoan.StartDate = dto.StartDate;
                existingLoan.EndDate = dto.EndDate;
            }
            
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

        public async Task<LoanDetailsDto> AmendLoanAsync(Guid id, AmendLoanDto dto, Guid userId)
        {
            var existingLoan = await loanRepository.GetByIdAsync(id);
            if (existingLoan == null || existingLoan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            // Create a new loan version for amendment
            var amendedLoan = new Loan(
                existingLoan.UserId,
                existingLoan.CounterpartyName,
                existingLoan.Principal,
                dto.InterestRate,
                dto.InterestType,
                existingLoan.StartDate  // StartDate is copied from existing loan
            )
            {
                CompoundingFrequency = dto.CompoundingFrequency,
                EndDate = dto.EndDate,
                RepaymentFrequency = dto.RepaymentFrequency,
                AllowOverpayment = dto.AllowOverpayment,
                Currency = dto.Currency,
                Status = existingLoan.Status,
                Notes = dto.Notes
            };

            var createdAmendment = await loanRepository.AmendAsync(id, amendedLoan);
            return mapper.Map<LoanDetailsDto>(createdAmendment);
        }

        public async Task<LoanDetailsDto> AmendLoanAsync(Guid id, UpdateLoanDto dto, Guid userId)
        {
            var amendDto = new AmendLoanDto
            {
                InterestRate = dto.InterestRate,
                InterestType = dto.InterestType,
                CompoundingFrequency = dto.CompoundingFrequency,
                EndDate = dto.EndDate,
                RepaymentFrequency = dto.RepaymentFrequency,
                AllowOverpayment = dto.AllowOverpayment,
                Currency = dto.Currency,
                Notes = dto.Notes
            };
            return await AmendLoanAsync(id, amendDto, userId);
        }
    }
}