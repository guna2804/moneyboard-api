using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.WebApi.Extensions;

namespace MoneyBoard.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoanController(ILoanService loanService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetLoans(
            [FromQuery] string? role,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var userId = GetCurrentUserId();
            var result = await loanService.GetLoansAsync(role, status, page, pageSize, userId);
            return ApiResponseHelper.OkResponse(result, "Loans retrieved successfully");
        }

        [HttpGet("{loanId}")]
        public async Task<IActionResult> GetLoanById(Guid loanId)
        {
            var userId = GetCurrentUserId();
            var result = await loanService.GetLoanByIdAsync(loanId, userId);
            return ApiResponseHelper.OkResponse(result, "Loan details with repayment history retrieved successfully");
        }

        [HttpGet("with-outstanding")]
        public async Task<IActionResult> GetLoansWithOutstanding(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var userId = GetCurrentUserId();
            var result = await loanService.GetLoansWithOutstandingRepaymentsAsync(userId, page, pageSize);
            return ApiResponseHelper.OkResponse(result, "Loans with outstanding repayments retrieved successfully");
        }

        [HttpPost]
        public async Task<IActionResult> CreateLoan(CreateLoanDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await loanService.CreateLoanAsync(dto, userId);
            return ApiResponseHelper.CreatedResponse(result, "Loan created successfully", nameof(GetLoanById), new { loanId = result.Id });
        }

        [HttpPut("{loanId}")]
        public async Task<IActionResult> UpdateLoan(Guid loanId, UpdateLoanDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await loanService.UpdateLoanAsync(loanId, dto, userId);
            return ApiResponseHelper.OkResponse(result, "Loan updated successfully");
        }

        [HttpDelete("{loanId}")]
        public async Task<IActionResult> DeleteLoan(Guid loanId)
        {
            var userId = GetCurrentUserId();
            await loanService.DeleteLoanAsync(loanId, userId);
            return ApiResponseHelper.NoContentResponse("Loan deleted successfully");
        }

        [HttpPost("{loanId}/amend")]
        public async Task<IActionResult> AmendLoan(Guid loanId, UpdateLoanDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await loanService.AmendLoanAsync(loanId, dto, userId);
            return ApiResponseHelper.CreatedResponse(result, "Loan amended successfully", nameof(GetLoanById), new { loanId = result.Id });
        }

        private Guid GetCurrentUserId()
        {
            // Extract user ID from JWT token claims
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token.");
            }
            return userId;
        }
    }
}