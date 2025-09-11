using AutoMapper;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Enums;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IMapper _mapper;

        public LoanService(ILoanRepository loanRepository, IMapper mapper)
        {
            _loanRepository = loanRepository;
            _mapper = mapper;
        }

        public async Task<LoanDetailsDto> GetLoanByIdAsync(Guid id, Guid userId)
        {
            var loan = await _loanRepository.GetByIdAsync(id);
            if (loan == null || loan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            var loanDto = _mapper.Map<LoanDetailsDto>(loan);
            loanDto.TotalAmount = loan.CalculateTotalAmount();

            return loanDto;
        }

        public async Task<PagedLoanResponseDto> GetLoansAsync(string? role, string? status, int page, int pageSize, Guid userId)
        {
            var loans = await _loanRepository.GetLoansAsync(role, status, page, pageSize, userId);
            var totalCount = await _loanRepository.GetTotalLoansCountAsync(role, status, userId);

            var loanDtos = _mapper.Map<IEnumerable<LoanDetailsDto>>(loans);

            foreach (var loanDto in loanDtos)
            {
                var loan = loans.FirstOrDefault(l => l.Id == loanDto.Id);
                if (loan != null)
                {
                    loanDto.TotalAmount = loan.CalculateTotalAmount();
                }
            }

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
            var loan = _mapper.Map<Loan>(dto);
            loan.UserId = userId;
            loan.StartDate = dto.StartDate;
            loan.EndDate = dto.EndDate;

            var createdLoan = await _loanRepository.CreateAsync(loan);
            var loanDto = _mapper.Map<LoanDetailsDto>(createdLoan);
            loanDto.TotalAmount = createdLoan.CalculateTotalAmount();

            return loanDto;
        }

        public async Task<LoanDetailsDto> UpdateLoanAsync(Guid id, UpdateLoanDto dto, Guid userId)
        {
            var existingLoan = await _loanRepository.GetByIdAsync(id);
            if (existingLoan == null || existingLoan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            if (existingLoan.HasRepaymentStarted)
            {
                // After first repayment: lock principal and start date, allow other edits
                existingLoan.CounterpartyName = dto.CounterpartyName;
                existingLoan.Role = dto.Role;
                existingLoan.InterestRate = dto.InterestRate;
                existingLoan.InterestType = dto.InterestType;
                existingLoan.EndDate = dto.EndDate;
                existingLoan.RepaymentFrequency = dto.RepaymentFrequency;
                existingLoan.AllowOverpayment = dto.AllowOverpayment;
                existingLoan.Currency = dto.Currency;
                existingLoan.Notes = dto.Notes;
                // Principal and StartDate are locked after first repayment - don't update them
            }
            else
            {
                // Before first repayment: allow all edits including principal and start date
                _mapper.Map(dto, existingLoan);
                existingLoan.StartDate = dto.StartDate;
                existingLoan.EndDate = dto.EndDate;
                // Explicitly set principal using method
                existingLoan.SetPrincipal(dto.Principal);
            }

            var updatedLoan = await _loanRepository.UpdateAsync(existingLoan);
            var loanDto = _mapper.Map<LoanDetailsDto>(updatedLoan);
            loanDto.TotalAmount = updatedLoan.CalculateTotalAmount();

            return loanDto;
        }

        public async Task DeleteLoanAsync(Guid id, Guid userId)
        {
            var existingLoan = await _loanRepository.GetByIdAsync(id);
            if (existingLoan == null || existingLoan.UserId != userId)
            {
                throw new KeyNotFoundException("Loan not found or access denied.");
            }

            await _loanRepository.SoftDeleteAsync(id);
        }

        public async Task<LoanDetailsDto> AmendLoanAsync(Guid id, AmendLoanDto dto, Guid userId)
        {
            var existingLoan = await _loanRepository.GetByIdAsync(id);
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
                EndDate = dto.EndDate,
                RepaymentFrequency = dto.RepaymentFrequency,
                AllowOverpayment = dto.AllowOverpayment,
                Currency = dto.Currency,
                Status = existingLoan.Status,
                Notes = dto.Notes
            };

            var createdAmendment = await _loanRepository.AmendAsync(id, amendedLoan);
            var loanDto = _mapper.Map<LoanDetailsDto>(createdAmendment);
            loanDto.TotalAmount = createdAmendment.CalculateTotalAmount();

            return loanDto;
        }

        public async Task<LoanDetailsDto> AmendLoanAsync(Guid id, UpdateLoanDto dto, Guid userId)
        {
            // Convert UpdateLoanDto to AmendLoanDto to avoid duplication
            var amendDto = new AmendLoanDto
            {
                InterestRate = dto.InterestRate,
                InterestType = dto.InterestType,
                EndDate = dto.EndDate,
                RepaymentFrequency = dto.RepaymentFrequency,
                AllowOverpayment = dto.AllowOverpayment,
                Currency = dto.Currency,
                Notes = dto.Notes
            };
            return await AmendLoanAsync(id, amendDto, userId);
        }

        public async Task<OutstandingLoansResponseDto> GetLoansWithOutstandingRepaymentsAsync(Guid userId, int page, int pageSize)
        {
            var activeLoans = await _loanRepository.GetActiveLoansAsync(userId);
            var loansWithOutstanding = activeLoans.Where(l => l.CalculateOutstandingBalance() > 0).ToList();
            var totalCount = loansWithOutstanding.Count;
            var pagedLoans = loansWithOutstanding.Skip((page - 1) * pageSize).Take(pageSize);

            var dtos = pagedLoans.Select(l => new OutstandingLoanDto
            {
                LoanId = l.Id,
                OutstandingBalance = l.CalculateOutstandingBalance(),
                InterestRate = l.InterestRate,
                Status = l.Status,
                AllowOverpayment = l.AllowOverpayment,
                NextDueDate = CalculateNextDueDate(l),
                EmiAmount = CalculateEmiAmount(l),
                BorrowerName = l.Role == LoanRole.Lender.ToString() ? l.CounterpartyName : null,
                LenderName = l.Role == LoanRole.Borrower.ToString() ? l.CounterpartyName : null,
                Role = l.Role
            }).ToList();

            return new OutstandingLoansResponseDto
            {
                Loans = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private DateOnly? CalculateNextDueDate(Loan loan)
        {
            var lastRepayment = loan.Repayments.Where(r => !r.IsDeleted).OrderByDescending(r => r.RepaymentDate).FirstOrDefault();
            DateOnly baseDate = lastRepayment != null ? DateOnly.FromDateTime(lastRepayment.RepaymentDate) : loan.StartDate;

            return loan.RepaymentFrequency switch
            {
                RepaymentFrequencyType.Monthly => baseDate.AddMonths(1),
                RepaymentFrequencyType.Quarterly => baseDate.AddMonths(3),
                RepaymentFrequencyType.Yearly => baseDate.AddYears(1),
                RepaymentFrequencyType.LumpSum => null,
                _ => baseDate
            };
        }

        private decimal CalculateEmiAmount(Loan loan)
        {
            if (loan.EndDate == null)
                return 0;

            int totalPeriods = CalculateNumberOfPeriods(loan);
            if (totalPeriods <= 0)
                return 0;

            decimal totalAmount = loan.CalculateTotalAmount();
            decimal outstandingBalance = loan.CalculateOutstandingBalance();

            decimal emi;
            if (loan.InterestType == InterestType.Compound)
            {
                // For compound, use original EMI calculation based on principal
                int periodsPerYear = loan.RepaymentFrequency switch
                {
                    RepaymentFrequencyType.Monthly => 12,
                    RepaymentFrequencyType.Quarterly => 4,
                    RepaymentFrequencyType.Yearly => 1,
                    _ => 12
                };

                decimal r = loan.InterestRate / 100 / periodsPerYear;
                if (r <= 0)
                    return 0;

                emi = loan.Principal * r * (decimal)Math.Pow(1 + (double)r, totalPeriods) / ((decimal)Math.Pow(1 + (double)r, totalPeriods) - 1);
            }
            else // Flat
            {
                // For flat, use fixed EMI based on total amount / total periods
                emi = totalAmount / totalPeriods;
            }

            emi = Math.Round(emi, 2, MidpointRounding.ToEven);

            // If outstanding balance is less than EMI, suggest paying the outstanding balance
            if (outstandingBalance < emi)
            {
                return outstandingBalance;
            }

            return emi;
        }

        private int CalculateNumberOfPeriods(Loan loan)
        {
            if (loan.EndDate == null)
                return 0;

            var start = loan.StartDate.ToDateTime(TimeOnly.MinValue);
            var end = loan.EndDate.Value.ToDateTime(TimeOnly.MinValue);

            int periodsPerYear = loan.RepaymentFrequency switch
            {
                RepaymentFrequencyType.Monthly => 12,
                RepaymentFrequencyType.Quarterly => 4,
                RepaymentFrequencyType.Yearly => 1,
                _ => 12
            };

            var totalDays = (end - start).TotalDays;
            var periods = (int)Math.Ceiling(totalDays / (365.0 / periodsPerYear));

            return periods;
        }

        private int CalculateRemainingPeriods(Loan loan, DateOnly nextDueDate)
        {
            var end = loan.EndDate.Value;
            int periodsPerYear = loan.RepaymentFrequency switch
            {
                RepaymentFrequencyType.Monthly => 12,
                RepaymentFrequencyType.Quarterly => 4,
                RepaymentFrequencyType.Yearly => 1,
                _ => 12
            };

            var totalDays = (end.ToDateTime(TimeOnly.MinValue) - nextDueDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
            return (int)Math.Ceiling(totalDays / (365.0 / periodsPerYear));
        }
    }
}
