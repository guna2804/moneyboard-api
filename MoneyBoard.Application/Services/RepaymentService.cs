using AutoMapper;
using Microsoft.Extensions.Logging;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Enums;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Application.Services
{
    public class RepaymentService : IRepaymentService
    {
        private readonly IRepaymentRepository _repaymentRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly ILogger<RepaymentService> _logger;

        public RepaymentService(
            IRepaymentRepository repaymentRepository,
            ILoanRepository loanRepository,
            IAuditService auditService,
            IMapper mapper,
            ILogger<RepaymentService> logger)
        {
            _repaymentRepository = repaymentRepository;
            _loanRepository = loanRepository;
            _auditService = auditService;
            _mapper = mapper;
            _logger = logger;
        }

        private int CalculateExpectedInstallments(Loan loan)
        {
            if (loan.EndDate == null) return int.MaxValue; // Unlimited if no end date

            var start = loan.StartDate;
            var end = loan.EndDate.Value;

            if (loan.RepaymentFrequency == Domain.Enums.RepaymentFrequencyType.LumpSum) return 1;

            int months = (end.Year - start.Year) * 12 + end.Month - start.Month;
            if (end.Day < start.Day) months--;

            if (months <= 0) return 0;

            return loan.RepaymentFrequency switch
            {
                Domain.Enums.RepaymentFrequencyType.Monthly => months,
                Domain.Enums.RepaymentFrequencyType.Quarterly => (int)Math.Ceiling(months / 3.0),
                Domain.Enums.RepaymentFrequencyType.Yearly => (int)Math.Ceiling(months / 12.0),
                _ => months
            };
        }

        public async Task<RepaymentResult> CreateRepaymentAsync(Guid loanId, CreateRepaymentRequestDto request, Guid userId)
        {
            _logger.LogInformation("Creating repayment for loan {LoanId}, amount: {Amount}", loanId, request.Amount);

            var loan = await _loanRepository.GetByIdAsync(loanId);
            if (loan == null)
                return RepaymentResult.Error("Loan not found.");

            if (loan.UserId != userId)
                return RepaymentResult.Error("Loan not found or access denied.");

            if (loan.Status == Domain.Enums.LoanStatus.Completed)
                return RepaymentResult.Error("Cannot create repayment for a completed loan.");

            if (request.RepaymentDate.Date < loan.StartDate.ToDateTime(TimeOnly.MinValue).Date)
                return RepaymentResult.Error("Repayment date cannot be before loan start date.");

            // Check repayment count against expected installments
            var existingCount = await _repaymentRepository.GetRepaymentCountAsync(loanId, null);
            var expected = CalculateExpectedInstallments(loan);
            if (!loan.AllowOverpayment && existingCount + 1 > expected)
                return RepaymentResult.Error("Repayment count exceeds expected schedule for this loan. Overpayment is not allowed.");

            // Additional date validations
            ValidateRepaymentDate(request.RepaymentDate, loan);

            var (interestPortion, principalPortion, remaining) =
                loan.AllocateRepayment(request.Amount, request.RepaymentDate, loan.AllowOverpayment);

            var nextDueDate = loan.GetNextDueDate();

            var repayment = new Repayment(
                loanId: loanId,
                amount: request.Amount,
                repaymentDate: request.RepaymentDate,
                interestComponent: interestPortion,
                principalComponent: principalPortion,
                nextDueDate: nextDueDate,
                notes: request.Notes
            );

            await _repaymentRepository.AddRepaymentAsync(repayment);
            await _repaymentRepository.SaveChangesAsync();

            // Update loan status after repayment
            loan.CalculateOutstandingBalance(request.RepaymentDate);
            await _loanRepository.UpdateAsync(loan);

            await _auditService.LogAuditAsync(
                "Repayment",
                repayment.Id.ToString(),
                "Create",
                userId,
                $"Created repayment: {repayment.Amount:N2}, Interest: {interestPortion:N2}, Principal: {principalPortion:N2}"
            );

            var dto = _mapper.Map<RepaymentResponseDto>(repayment);
            dto.NewBalance = loan.CalculateOutstandingBalance(request.RepaymentDate);
            return RepaymentResult.Success(dto);
        }

        public async Task<RepaymentResult> UpdateRepaymentAsync(Guid loanId, Guid repaymentId, UpdateRepaymentRequestDto request, Guid userId)
        {
            _logger.LogInformation("Updating repayment {RepaymentId} for loan {LoanId}", repaymentId, loanId);

            var loan = await _loanRepository.GetByIdAsync(loanId);
            if (loan == null)
                return RepaymentResult.Error("Loan not found.");

            if (loan.UserId != userId)
                return RepaymentResult.Error("Loan not found or access denied.");

            if (loan.Status == Domain.Enums.LoanStatus.Completed)
                return RepaymentResult.Error("Cannot update repayment for a completed loan.");

            var repayment = await _repaymentRepository.GetByIdAsync(repaymentId);
            if (repayment == null)
                return RepaymentResult.Error("Repayment not found.");

            if (repayment.LoanId != loanId)
                return RepaymentResult.Error("Repayment not found for the provided loan.");

            var oldAmount = repayment.Amount;


            // Additional date validations
            ValidateRepaymentDate(request.RepaymentDate, loan);

            var (interestPortion, principalPortion, remaining) =
                loan.AllocateRepayment(request.Amount, request.RepaymentDate, loan.AllowOverpayment);

            var nextDueDate = loan.GetNextDueDate();

            repayment.Update(
                amount: request.Amount,
                repaymentDate: request.RepaymentDate,
                notes: request.Notes,
                interestComponent: interestPortion,
                principalComponent: principalPortion,
                nextDueDate: nextDueDate
            );

            await _repaymentRepository.UpdateRepaymentAsync(repayment);
            await _repaymentRepository.SaveChangesAsync();

            // Update loan status after repayment update
            loan.CalculateOutstandingBalance(request.RepaymentDate);
            await _loanRepository.UpdateAsync(loan);

            await _auditService.LogAuditAsync(
                "Repayment",
                repaymentId.ToString(),
                "Update",
                userId,
                $"Updated repayment from {oldAmount:N2} to {request.Amount:N2}"
            );

            var dto = _mapper.Map<RepaymentResponseDto>(repayment);
            dto.NewBalance = loan.CalculateOutstandingBalance(request.RepaymentDate);
            return RepaymentResult.Success(dto);
        }

        public async Task<PagedRepaymentResponseDto> GetRepaymentsAsync(Guid loanId, int page, int pageSize, string? sortBy, string? filter, Guid userId)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.UserId != userId)
                throw new KeyNotFoundException("Loan not found or access denied.");

            var repayments = await _repaymentRepository.GetRepaymentsByLoanIdAsync(loanId, page, pageSize, sortBy, filter);
            var totalCount = await _repaymentRepository.GetRepaymentCountAsync(loanId, filter);

            return new PagedRepaymentResponseDto
            {
                Repayments = _mapper.Map<IEnumerable<RepaymentDto>>(repayments),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task DeleteRepaymentAsync(Guid loanId, Guid repaymentId, Guid userId)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.UserId != userId)
                throw new KeyNotFoundException("Loan not found or access denied.");

            var repayment = await _repaymentRepository.GetByIdAsync(repaymentId)
                ?? throw new KeyNotFoundException("Repayment not found.");

            if (repayment.LoanId != loanId)
                throw new KeyNotFoundException("Repayment not found for the provided loan.");

            repayment.SoftDelete();

            await _repaymentRepository.UpdateRepaymentAsync(repayment);
            await _repaymentRepository.SaveChangesAsync();

            // Update loan status after repayment deletion
            loan.CalculateOutstandingBalance(DateTime.UtcNow);
            await _loanRepository.UpdateAsync(loan);

            await _auditService.LogAuditAsync(
                "Repayment",
                repaymentId.ToString(),
                "Delete",
                userId,
                $"Marked repayment as deleted: {repayment.Amount:N2}"
            );
        }


        private static void ValidateRepaymentDate(DateTime repaymentDate, Loan loan)
        {
            var now = DateTime.UtcNow;

            // Repayment date cannot be more than 1 year in the future
            if (repaymentDate > now.AddYears(1))
                throw new InvalidOperationException("Repayment date cannot be more than 1 year in the future.");

            // For lump sum loans, repayment date should be reasonable
            if (loan.RepaymentFrequency == Domain.Enums.RepaymentFrequencyType.LumpSum)
            {
                var loanEndDate = loan.EndDate ?? loan.StartDate.AddYears(1);
                if (repaymentDate > loanEndDate.ToDateTime(TimeOnly.MinValue).AddMonths(1))
                    throw new InvalidOperationException("Lump sum repayment date should be within a reasonable time after the loan end date.");
            }

            // Repayment date should not be before the loan's first possible repayment date
            var firstPossibleRepayment = loan.StartDate.ToDateTime(TimeOnly.MinValue);
            if (repaymentDate < firstPossibleRepayment)
                throw new InvalidOperationException("Repayment date cannot be before the loan's start date.");

            // Repayment date should not be after the loan's end date plus grace period
            if (loan.EndDate.HasValue)
            {
                var endDateTime = loan.EndDate.Value.ToDateTime(TimeOnly.MinValue);
                var graceEnd = endDateTime.AddDays(30);
                if (repaymentDate > graceEnd)
                    throw new InvalidOperationException("Repayment date is outside the loan's valid period.");
            }
        }

        public async Task<RepaymentSummaryDto> GetRepaymentSummaryAsync(string role, Guid userId)
        {
            _logger.LogInformation("Getting repayment summary for user {UserId} with role filter: {Role}", userId, role);

            // Validate role parameter
            if (role != "lending" && role != "borrowing" && role != "all")
                throw new ArgumentException("Invalid role parameter. Must be 'lending', 'borrowing', or 'all'");

            var summary = new RepaymentSummaryDto();

            if (role == "lending" || role == "all")
            {
                // Get lending summary using optimized database query
                var lendingData = await _repaymentRepository.GetRepaymentSummaryDataAsync(userId, "lending");
                var lendingBreakdown = new RepaymentBreakdownDto
                {
                    TotalPayments = lendingData.TotalPayments,
                    TotalInterest = lendingData.TotalInterest,
                    TotalPrincipal = lendingData.TotalPrincipal
                };

                if (role == "lending")
                {
                    summary.TotalPayments = lendingBreakdown.TotalPayments;
                    summary.TotalInterest = lendingBreakdown.TotalInterest;
                    summary.TotalPrincipal = lendingBreakdown.TotalPrincipal;
                }
                else // role == "all"
                {
                    summary.LendingBreakdown = lendingBreakdown;
                }
            }

            if (role == "borrowing" || role == "all")
            {
                // Get borrowing summary using optimized database query
                var borrowingData = await _repaymentRepository.GetRepaymentSummaryDataAsync(userId, "borrowing");
                var borrowingBreakdown = new RepaymentBreakdownDto
                {
                    TotalPayments = borrowingData.TotalPayments,
                    TotalInterest = borrowingData.TotalInterest,
                    TotalPrincipal = borrowingData.TotalPrincipal
                };

                if (role == "borrowing")
                {
                    summary.TotalPayments = borrowingBreakdown.TotalPayments;
                    summary.TotalInterest = borrowingBreakdown.TotalInterest;
                    summary.TotalPrincipal = borrowingBreakdown.TotalPrincipal;
                }
                else // role == "all"
                {
                    summary.BorrowingBreakdown = borrowingBreakdown;
                    // Combine totals for "all" role
                    summary.TotalPayments = (summary.LendingBreakdown?.TotalPayments ?? 0) + borrowingBreakdown.TotalPayments;
                    summary.TotalInterest = (summary.LendingBreakdown?.TotalInterest ?? 0) + borrowingBreakdown.TotalInterest;
                    summary.TotalPrincipal = (summary.LendingBreakdown?.TotalPrincipal ?? 0) + borrowingBreakdown.TotalPrincipal;
                }
            }

            return summary;
        }
    }
}