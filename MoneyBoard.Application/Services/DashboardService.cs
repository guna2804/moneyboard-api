using Microsoft.Extensions.Logging;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IRepaymentRepository _repaymentRepository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ILoanRepository loanRepository,
            IRepaymentRepository repaymentRepository,
            ILogger<DashboardService> logger)
        {
            _loanRepository = loanRepository;
            _repaymentRepository = repaymentRepository;
            _logger = logger;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // Calculate date ranges for current and last month
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);

            // Get current month totals
            var totalLent = await _loanRepository.GetTotalLentAsync(userId, currentMonthStart, currentMonthEnd);
            var totalBorrowed = await _loanRepository.GetTotalBorrowedAsync(userId, currentMonthStart, currentMonthEnd);
            var interestEarned = await _loanRepository.GetTotalInterestEarnedAsync(userId, currentMonthStart, currentMonthEnd);

            // Get last month totals for comparison
            var lastMonthLent = await _loanRepository.GetTotalLentAsync(userId, lastMonthStart, lastMonthEnd);
            var lastMonthBorrowed = await _loanRepository.GetTotalBorrowedAsync(userId, lastMonthStart, lastMonthEnd);
            var lastMonthInterest = await _loanRepository.GetTotalInterestEarnedAsync(userId, lastMonthStart, lastMonthEnd);

            // Calculate percentage changes
            var lentChangePercent = lastMonthLent != 0 ? ((totalLent - lastMonthLent) / lastMonthLent) * 100 : 0;
            var borrowedChangePercent = lastMonthBorrowed != 0 ? ((totalBorrowed - lastMonthBorrowed) / lastMonthBorrowed) * 100 : 0;
            var interestChangePercent = lastMonthInterest != 0 ? ((interestEarned - lastMonthInterest) / lastMonthInterest) * 100 : 0;

            return new DashboardSummaryDto
            {
                TotalLent = totalLent,
                LentChangePercent = Math.Round(lentChangePercent, 1),
                TotalBorrowed = totalBorrowed,
                BorrowedChangePercent = Math.Round(borrowedChangePercent, 1),
                InterestEarned = interestEarned,
                InterestChangePercent = Math.Round(interestChangePercent, 1)
            };
        }

        public async Task<RecentTransactionsResponseDto> GetRecentTransactionsAsync(Guid userId, int limit = 5, int page = 1, CancellationToken ct = default)
        {
            var offset = (page - 1) * limit;

            // Get total count for pagination
            var totalCount = await _repaymentRepository.GetRecentRepaymentsCountByUserAsync(userId);

            // Get paginated recent repayments with loan information
            var recentRepayments = await _repaymentRepository.GetRecentRepaymentsByUserAsync(userId, limit, offset);

            var transactions = recentRepayments.Select(r =>
            {
                var direction = r.Loan!.Role == "Lender" ? "in" : "out";
                var status = r.Status.ToString().ToLower();
                return new RecentTransactionDto
                {
                    Id = r.Id.ToString(),
                    Name = r.Loan.CounterpartyName,
                    DueDate = r.RepaymentDate.ToString("yyyy-MM-dd"),
                    Amount = r.Amount,
                    Status = status,
                    Direction = direction
                };
            }).ToList();

            return new RecentTransactionsResponseDto
            {
                Transactions = transactions,
                Pagination = new PaginationDto
                {
                    Total = totalCount,
                    Page = page,
                    Limit = limit,
                    HasNext = (page * limit) < totalCount
                }
            };
        }

        public async Task<UpcomingPaymentsResponseDto> GetUpcomingPaymentsAsync(Guid userId, int limit = 5, int page = 1, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var farFuture = now.AddYears(1); // Look ahead 1 year for upcoming payments

            // Get active loans that might have upcoming payments
            var activeLoans = await _loanRepository.GetLoansWithUpcomingPaymentsAsync(userId, now, farFuture);
            var upcomingPayments = new List<(Loan loan, DateTime dueDate, decimal amount)>();

            foreach (var loan in activeLoans)
            {
                if (loan.Status == Domain.Enums.LoanStatus.Active)
                {
                    var nextDueDate = loan.GetNextDueDate();
                    if (nextDueDate > now)
                    {
                        var emiAmount = loan.CalculateEmiAmount();
                        upcomingPayments.Add((loan, nextDueDate, emiAmount));
                    }
                }
            }

            var sortedPayments = upcomingPayments
                .OrderBy(p => p.dueDate)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            var payments = sortedPayments.Select(p => new UpcomingPaymentDto
            {
                Id = p.loan.Id.ToString(),
                Name = p.loan.CounterpartyName,
                DueDate = p.dueDate.ToString("yyyy-MM-dd"),
                Amount = p.amount,
                Direction = p.loan.Role == "Lender" ? "in" : "out"
            }).ToList();

            return new UpcomingPaymentsResponseDto
            {
                UpcomingPayments = payments,
                Pagination = new PaginationDto
                {
                    Total = upcomingPayments.Count,
                    Page = page,
                    Limit = limit,
                    HasNext = (page * limit) < upcomingPayments.Count
                }
            };
        }

        public async Task<List<MonthlyRepaymentDto>> GetMonthlyRepaymentsAsync(Guid userId, int year, CancellationToken ct = default)
        {
            // Get monthly totals directly from database
            var monthlyTotals = await _loanRepository.GetMonthlyRepaymentTotalsAsync(userId, year);

            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            return months.Select(m =>
            {
                var monthNumber = Array.IndexOf(months, m) + 1;
                var amount = monthlyTotals.GetValueOrDefault(monthNumber.ToString(), 0);
                return new MonthlyRepaymentDto
                {
                    Month = m,
                    Amount = amount
                };
            }).ToList();
        }

        public async Task<LoanStatusDistributionDto> GetLoanStatusDistributionAsync(Guid userId, CancellationToken ct = default)
        {
            var distributionData = await _loanRepository.GetLoanStatusDistributionAsync(userId);

            return new LoanStatusDistributionDto
            {
                Active = distributionData.GetValueOrDefault("Active", 0),
                Closed = distributionData.GetValueOrDefault("Completed", 0),
                Overdue = distributionData.GetValueOrDefault("Overdue", 0)
            };
        }

        public async Task<List<AlertDto>> GetAlertsAsync(Guid userId, CancellationToken ct = default)
        {
            var alerts = new List<AlertDto>();
            var now = DateTime.UtcNow;
            var sevenDaysFromNow = now.AddDays(7);

            // Get overdue loans count efficiently
            var overdueLoans = await _loanRepository.GetOverdueLoansAsync(userId);
            if (overdueLoans.Any())
            {
                alerts.Add(new AlertDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "overdue",
                    Message = $"You have {overdueLoans.Count()} overdue payments.",
                    Link = "/repayments?filter=overdue"
                });
            }

            // Get loans with upcoming due dates
            var loansWithUpcoming = await _loanRepository.GetLoansWithUpcomingDueDatesAsync(userId, now, sevenDaysFromNow);
            var upcomingCount = 0;

            foreach (var loan in loansWithUpcoming)
            {
                var nextDue = loan.GetNextDueDate();
                if (nextDue >= now && nextDue <= sevenDaysFromNow)
                {
                    upcomingCount++;
                }
            }

            if (upcomingCount > 0)
            {
                alerts.Add(new AlertDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "upcoming",
                    Message = $"{upcomingCount} payments due in the next 7 days.",
                    Link = "/upcoming-payments"
                });
            }

            return alerts;
        }
    }
}