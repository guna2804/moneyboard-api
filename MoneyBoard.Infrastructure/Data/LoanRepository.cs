using Microsoft.EntityFrameworkCore;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Enums;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Infrastructure.Data
{
    public class LoanRepository(AppDbContext context) : ILoanRepository
    {
        public async Task<Loan?> GetByIdAsync(Guid id)
        {
            return await context.Loans
                .Include(l => l.User)
                .Include(l => l.Repayments)
                .Include(l => l.Notifications)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        }

        public async Task<IEnumerable<Loan>> GetLoansAsync(string? role, string? status, int page, int pageSize, Guid userId)
        {
            var query = context.Loans
                .Include(l => l.Repayments)
                .Where(l => !l.IsDeleted && l.UserId == userId);

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(l => l.Role == role);
            }

            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
                if (Enum.TryParse<LoanStatus>(status, true, out var loanStatus))
                    query = query.Where(l => l.Status == loanStatus);
            }

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalLoansCountAsync(string? role, string? status, Guid userId)
        {
            var query = context.Loans.AsNoTracking().Where(l => !l.IsDeleted && l.UserId == userId);

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(l => l.Role == role);
            }

            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
                if (Enum.TryParse<LoanStatus>(status, true, out var loanStatus))
                    query = query.Where(l => l.Status == loanStatus);
            }

            return await query.CountAsync();
        }

        public async Task<Loan> CreateAsync(Loan loan)
        {
            context.Loans.Add(loan);
            await context.SaveChangesAsync();
            return loan;
        }

        public async Task<Loan> UpdateAsync(Loan loan)
        {
            loan.SetUpdated();
            context.Loans.Update(loan);
            await context.SaveChangesAsync();
            return loan;
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            var loan = await context.Loans.FindAsync(id);
            if (loan != null && !loan.IsDeleted)
            {
                loan.SetDeleted();
                await context.SaveChangesAsync();
            }
        }

        public async Task<Loan> AmendAsync(Guid id, Loan amendment)
        {
            // Get the original loan with repayments
            var originalLoan = await context.Loans
                .Include(l => l.Repayments.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (originalLoan == null)
                throw new KeyNotFoundException("Original loan not found.");

            // Soft delete the original loan
            originalLoan.SetDeleted();
            context.Loans.Update(originalLoan);

            // Create the amended loan
            context.Loans.Add(amendment);

            // Copy existing repayments to the new loan (only non-deleted ones)
            foreach (var repayment in originalLoan.Repayments.Where(r => !r.IsDeleted))
            {
                var nextDueDate = amendment.GetNextDueDate();
                var newRepayment = new Repayment(
                    loanId: amendment.Id,
                    amount: repayment.Amount,
                    repaymentDate: repayment.RepaymentDate,
                    interestComponent: repayment.InterestComponent,
                    principalComponent: repayment.PrincipalComponent,
                    nextDueDate: nextDueDate,
                    notes: repayment.Notes
                );
                // Note: New repayment will get current timestamps since it's a new entity
                context.Repayments.Add(newRepayment);
            }

            await context.SaveChangesAsync();
            return amendment;
        }

        public async Task<IEnumerable<Loan>> GetActiveLoansAsync(Guid userId)
        {
            return await context.Loans
                .Include(l => l.User)
                .Include(l => l.Repayments)
                .Where(l => !l.IsDeleted && l.UserId == userId && l.Status != LoanStatus.Completed)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalLentAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            return await context.Loans
                .Where(l => !l.IsDeleted &&
                           l.UserId == userId &&
                           l.Role == "Lender" &&
                           l.CreatedAt >= startDate &&
                           l.CreatedAt <= endDate)
                .SumAsync(l => l.Principal);
        }

        public async Task<decimal> GetTotalBorrowedAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            return await context.Loans
                .Where(l => !l.IsDeleted &&
                           l.UserId == userId &&
                           l.Role == "Borrower" &&
                           l.CreatedAt >= startDate &&
                           l.CreatedAt <= endDate)
                .SumAsync(l => l.Principal);
        }

        public async Task<decimal> GetTotalInterestEarnedAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            var lenderLoans = await context.Loans
                .Include(l => l.Repayments)
                .Where(l => !l.IsDeleted &&
                           l.UserId == userId &&
                           l.Role == "Lender" &&
                           l.CreatedAt >= startDate &&
                           l.CreatedAt <= endDate)
                .ToListAsync();

            decimal totalInterest = 0;
            foreach (var loan in lenderLoans)
            {
                totalInterest += loan.Repayments
                    .Where(r => !r.IsDeleted && r.RepaymentDate >= startDate && r.RepaymentDate <= endDate)
                    .Sum(r => r.InterestComponent);
            }

            return totalInterest;
        }

        public async Task<IEnumerable<Loan>> GetLoansWithUpcomingPaymentsAsync(Guid userId, DateTime fromDate, DateTime toDate)
        {
            // This is complex to implement efficiently in SQL due to the business logic in GetNextDueDate()
            // For now, we'll get active loans and calculate in memory, but this could be optimized later
            return await context.Loans
                .Where(l => !l.IsDeleted &&
                           l.UserId == userId &&
                           l.Status == LoanStatus.Active)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetLoanStatusDistributionAsync(Guid userId)
        {
            var distribution = await context.Loans
                .Where(l => !l.IsDeleted && l.UserId == userId)
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status.ToString(), g => g.Count);

            return distribution;
        }

        public async Task<Dictionary<string, decimal>> GetMonthlyRepaymentTotalsAsync(Guid userId, int year)
        {
            var monthlyTotals = await context.Repayments
                .Include(r => r.Loan)
                .Where(r => !r.IsDeleted &&
                           r.Loan != null &&
                           !r.Loan.IsDeleted &&
                           r.Loan.UserId == userId &&
                           r.RepaymentDate.Year == year)
                .GroupBy(r => r.RepaymentDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(r => r.Amount) })
                .ToDictionaryAsync(g => g.Month.ToString(), g => g.Total);

            return monthlyTotals;
        }

        public async Task<IEnumerable<Loan>> GetOverdueLoansAsync(Guid userId)
        {
            return await context.Loans
                .Where(l => !l.IsDeleted &&
                           l.UserId == userId &&
                           l.Status == LoanStatus.Overdue)
                .ToListAsync();
        }

        public async Task<IEnumerable<Loan>> GetLoansWithUpcomingDueDatesAsync(Guid userId, DateTime fromDate, DateTime toDate)
        {
            // Similar to GetLoansWithUpcomingPaymentsAsync, this requires business logic
            // For now, return active loans and calculate in service layer
            return await context.Loans
                .Where(l => !l.IsDeleted &&
                           l.UserId == userId &&
                           l.Status == LoanStatus.Active)
                .ToListAsync();
        }
    }
}
