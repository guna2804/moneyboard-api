using Microsoft.EntityFrameworkCore;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Enums;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Infrastructure.Data
{
    public class RepaymentRepository(AppDbContext context) : IRepaymentRepository
    {
        public async Task<Repayment?> GetByIdAsync(Guid id)
        {
            return await context.Repayments
                .Include(r => r.Loan)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        public async Task<IEnumerable<Repayment>> GetRepaymentsByLoanIdAsync(Guid loanId, int page, int pageSize, string? sortBy = null, string? filter = null)
        {
            var query = context.Repayments
                .Include(r => r.Loan)
                .Where(r => r.LoanId == loanId && !r.IsDeleted);

            // Apply filters
            if (!string.IsNullOrEmpty(filter))
            {
                // Simple date range filter: "startDate-endDate"
                if (filter.Contains("-"))
                {
                    var dates = filter.Split('-');
                    if (dates.Length == 2 &&
                        DateTime.TryParse(dates[0], out var startDate) &&
                        DateTime.TryParse(dates[1], out var endDate))
                    {
                        query = query.Where(r => r.RepaymentDate >= startDate && r.RepaymentDate <= endDate);
                    }
                }
                // Amount range filter: "minAmount-maxAmount"
                else if (filter.Contains(","))
                {
                    var amounts = filter.Split(',');
                    if (amounts.Length == 2 &&
                        decimal.TryParse(amounts[0], out var minAmount) &&
                        decimal.TryParse(amounts[1], out var maxAmount))
                    {
                        query = query.Where(r => r.Amount >= minAmount && r.Amount <= maxAmount);
                    }
                }
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "date" => query.OrderBy(r => r.RepaymentDate),
                "date_desc" => query.OrderByDescending(r => r.RepaymentDate),
                "amount" => query.OrderBy(r => r.Amount),
                "amount_desc" => query.OrderByDescending(r => r.Amount),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetRepaymentCountAsync(Guid loanId, string? filter = null)
        {
            var query = context.Repayments
                .Where(r => r.LoanId == loanId && !r.IsDeleted);

            // Apply same filters as GetRepaymentsByLoanIdAsync
            if (!string.IsNullOrEmpty(filter))
            {
                if (filter.Contains("-"))
                {
                    var dates = filter.Split('-');
                    if (dates.Length == 2 &&
                        DateTime.TryParse(dates[0], out var startDate) &&
                        DateTime.TryParse(dates[1], out var endDate))
                    {
                        query = query.Where(r => r.RepaymentDate >= startDate && r.RepaymentDate <= endDate);
                    }
                }
                else if (filter.Contains(","))
                {
                    var amounts = filter.Split(',');
                    if (amounts.Length == 2 &&
                        decimal.TryParse(amounts[0], out var minAmount) &&
                        decimal.TryParse(amounts[1], out var maxAmount))
                    {
                        query = query.Where(r => r.Amount >= minAmount && r.Amount <= maxAmount);
                    }
                }
            }

            return await query.CountAsync();
        }

        public async Task AddRepaymentAsync(Repayment repayment)
        {
            await context.Repayments.AddAsync(repayment);
        }

        public async Task UpdateRepaymentAsync(Repayment repayment)
        {
            repayment.SetUpdated();
            context.Repayments.Update(repayment);
        }

        public async Task SoftDeleteRepaymentAsync(Guid id)
        {
            var repayment = await context.Repayments.FindAsync(id);
            if (repayment != null && !repayment.IsDeleted)
            {
                repayment.SoftDelete();
                await UpdateRepaymentAsync(repayment);
            }
        }

        public async Task<IEnumerable<Repayment>> GetRepaymentsByUserRoleAsync(Guid userId, string userRole)
        {
            return await context.Repayments
                .Include(r => r.Loan)
                .Where(r => !r.IsDeleted &&
                           r.Loan != null &&
                           !r.Loan.IsDeleted &&
                           r.Loan.UserId == userId &&
                           r.Loan.Role == userRole)
                .ToListAsync();
        }

        public Task<int> SaveChangesAsync() => context.SaveChangesAsync();
    }
}