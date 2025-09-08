using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MoneyBoard.Application.Interfaces;

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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Database or calculation error.", error = ex.Message });
            }
        }

        [HttpGet("recent-transactions")]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int limit = 5, [FromQuery] int page = 1)
        {
            if (limit < 1 || limit > 20 || page < 1)
            {
                return BadRequest(new { message = "Invalid query parameters. Limit must be 1-20, page >= 1." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetRecentTransactionsAsync(userId, limit, page);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving recent transactions.", error = ex.Message });
            }
        }

        [HttpGet("upcoming-payments")]
        public async Task<IActionResult> GetUpcomingPayments([FromQuery] int limit = 5, [FromQuery] int page = 1)
        {
            if (limit < 1 || limit > 20 || page < 1)
            {
                return BadRequest(new { message = "Invalid query parameters. Limit must be 1-20, page >= 1." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetUpcomingPaymentsAsync(userId, limit, page);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving upcoming payments.", error = ex.Message });
            }
        }

        [HttpGet("monthly-repayments")]
        public async Task<IActionResult> GetMonthlyRepayments([FromQuery] int year)
        {
            if (year < 2000 || year > DateTime.UtcNow.Year + 10)
            {
                return BadRequest(new { message = "Invalid year parameter." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetMonthlyRepaymentsAsync(userId, year);
                return Ok(new { monthlyRepayments = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving monthly repayments.", error = ex.Message });
            }
        }

        [HttpGet("loan-status-distribution")]
        public async Task<IActionResult> GetLoanStatusDistribution()
        {
            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetLoanStatusDistributionAsync(userId);
                return Ok(new { distribution = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Aggregation error.", error = ex.Message });
            }
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            try
            {
                var userId = GetUserId();
                var result = await _dashboardService.GetAlertsAsync(userId);
                return Ok(new { alerts = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving alerts.", error = ex.Message });
            }
        }
    }
}