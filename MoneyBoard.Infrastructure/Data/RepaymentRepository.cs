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

        public Task UpdateRepaymentAsync(Repayment repayment)
        {
            repayment.SetUpdated();
            context.Repayments.Update(repayment);
            return Task.CompletedTask;
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

        public async Task<IEnumerable<Repayment>> GetRecentRepaymentsByUserAsync(Guid userId, int limit, int offset)
        {
            return await context.Repayments
                .Include(r => r.Loan)
                .Where(r => !r.IsDeleted &&
                           r.Loan != null &&
                           !r.Loan.IsDeleted &&
                           r.Loan.UserId == userId)
                .OrderByDescending(r => r.RepaymentDate)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetRecentRepaymentsCountByUserAsync(Guid userId)
        {
            return await context.Repayments
                .Include(r => r.Loan)
                .Where(r => !r.IsDeleted &&
                           r.Loan != null &&
                           !r.Loan.IsDeleted &&
                           r.Loan.UserId == userId)
                .CountAsync();
        }

        public async Task<bool> HasRepaymentInPeriodAsync(Guid loanId, DateTime repaymentDate, RepaymentFrequencyType frequency, Guid? excludeRepaymentId = null)
        {
            var query = context.Repayments
                .Where(r => !r.IsDeleted && r.LoanId == loanId);

            // Exclude the current repayment if updating
            if (excludeRepaymentId.HasValue)
            {
                query = query.Where(r => r.Id != excludeRepaymentId.Value);
            }

            return frequency switch
            {
                RepaymentFrequencyType.Monthly =>
                    await query.AnyAsync(r => r.RepaymentDate.Year == repaymentDate.Year && r.RepaymentDate.Month == repaymentDate.Month),

                RepaymentFrequencyType.Quarterly =>
                    await query.AnyAsync(r => GetQuarter(r.RepaymentDate) == GetQuarter(repaymentDate) && r.RepaymentDate.Year == repaymentDate.Year),

                RepaymentFrequencyType.Yearly =>
                    await query.AnyAsync(r => r.RepaymentDate.Year == repaymentDate.Year),

                RepaymentFrequencyType.LumpSum =>
                    await query.AnyAsync(), // Any existing repayment means lump sum is already paid

                _ => false
            };
        }

        public async Task<RepaymentSummaryData> GetRepaymentSummaryDataAsync(Guid userId, string role)
        {
            var query = context.Repayments
                .Include(r => r.Loan)
                .Where(r => !r.IsDeleted &&
                           r.Loan != null &&
                           !r.Loan.IsDeleted &&
                           r.Loan.UserId == userId);

            // Apply role filter
            if (role == "lending")
            {
                query = query.Where(r => r.Loan.Role == "Lender");
            }
            else if (role == "borrowing")
            {
                query = query.Where(r => r.Loan.Role == "Borrower");
            }
            // For "all", no additional filter needed

            var summary = await query
                .GroupBy(r => 1) // Group all results together
                .Select(g => new
                {
                    TotalPayments = g.Sum(r => r.Amount),
                    TotalInterest = g.Sum(r => r.InterestComponent),
                    TotalPrincipal = g.Sum(r => r.PrincipalComponent)
                })
                .FirstOrDefaultAsync();

            return summary != null
                ? new RepaymentSummaryData
                {
                    TotalPayments = summary.TotalPayments,
                    TotalInterest = summary.TotalInterest,
                    TotalPrincipal = summary.TotalPrincipal
                }
                : new RepaymentSummaryData();
        }

        private static int GetQuarter(DateTime date)
        {
            return (date.Month - 1) / 3 + 1;
        }
    }
}