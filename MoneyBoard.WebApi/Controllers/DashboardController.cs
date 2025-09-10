using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.WebApi.Extensions;

namespace MoneyBoard.WebApi.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token.");
            }
            return userId;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetSummaryAsync(userId);
                return ApiResponseHelper.OkResponse(result, "Dashboard summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponseHelper.InternalServerErrorResponse("Database or calculation error.");
            }
        }

        [HttpGet("recent-transactions")]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int limit = 5, [FromQuery] int page = 1)
        {
            if (limit < 1 || limit > 20 || page < 1)
            {
                return ApiResponseHelper.BadRequestResponse("Invalid query parameters. Limit must be 1-20, page >= 1.");
            }

            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetRecentTransactionsAsync(userId, limit, page);
                return ApiResponseHelper.OkResponse(result, "Recent transactions retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponseHelper.InternalServerErrorResponse("Error retrieving recent transactions.");
            }
        }

        [HttpGet("upcoming-payments")]
        public async Task<IActionResult> GetUpcomingPayments([FromQuery] int limit = 5, [FromQuery] int page = 1)
        {
            if (limit < 1 || limit > 20 || page < 1)
            {
                return ApiResponseHelper.BadRequestResponse("Invalid query parameters. Limit must be 1-20, page >= 1.");
            }

            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetUpcomingPaymentsAsync(userId, limit, page);
                return ApiResponseHelper.OkResponse(result, "Upcoming payments retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponseHelper.InternalServerErrorResponse("Error retrieving upcoming payments.");
            }
        }

        [HttpGet("monthly-repayments")]
        public async Task<IActionResult> GetMonthlyRepayments([FromQuery] int year)
        {
            if (year < 2000 || year > DateTime.UtcNow.Year + 10)
            {
                return ApiResponseHelper.BadRequestResponse("Invalid year parameter.");
            }

            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetMonthlyRepaymentsAsync(userId, year);
                return ApiResponseHelper.OkResponse(new { monthlyRepayments = result }, "Monthly repayments retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponseHelper.InternalServerErrorResponse("Error retrieving monthly repayments.");
            }
        }

        [HttpGet("loan-status-distribution")]
        public async Task<IActionResult> GetLoanStatusDistribution()
        {
            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetLoanStatusDistributionAsync(userId);
                return ApiResponseHelper.OkResponse(new { distribution = result }, "Loan status distribution retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponseHelper.InternalServerErrorResponse("Aggregation error.");
            }
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetAlertsAsync(userId);
                return ApiResponseHelper.OkResponse(new { alerts = result }, "Alerts retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponseHelper.InternalServerErrorResponse("Error retrieving alerts.");
            }
        }
    }
}