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

        public async Task<IEnumerable<Loan>> GetLoansAsync(string? role, string? status, int page, int pageSize)
        {
            var query = context.Loans
                .Include(l => l.User)
                .Include(l => l.Repayments)
                .Include(l => l.Notifications)
                .Where(l => !l.IsDeleted);

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(l => l.Role == role);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<LoanStatus>(status, true, out var loanStatus))
                {
                    query = query.Where(l => l.Status == loanStatus);
                }
            }

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalLoansCountAsync(string? role, string? status)
        {
            var query = context.Loans.Where(l => !l.IsDeleted);

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(l => l.Role == role);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<LoanStatus>(status, true, out var loanStatus))
                {
                    query = query.Where(l => l.Status == loanStatus);
                }
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
            // Soft delete the original loan
            var originalLoan = await context.Loans.FindAsync(id);
            if (originalLoan != null)
            {
                originalLoan.SetDeleted();
            }

            // Create the amended loan with incremented version
            context.Loans.Add(amendment);
            await context.SaveChangesAsync();
            return amendment;
        }
    }
}