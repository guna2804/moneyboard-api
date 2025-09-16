using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Application.Validators;
using MoneyBoard.WebApi.Extensions;

namespace MoneyBoard.WebApi.Controllers
{
    [Route("api/loans/{loanId}/[controller]")]
    [ApiController]
    [Authorize]
    public class RepaymentController(IRepaymentService repaymentService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetRepayments(
            Guid loanId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? filter = null)
        {
            var userId = GetCurrentUserId();
            var result = await repaymentService.GetRepaymentsAsync(loanId, page, pageSize, sortBy, filter, userId);
            return ApiResponseHelper.OkResponse(result, "Repayments retrieved successfully");
        }

        [HttpPost]
        public async Task<IActionResult> CreateRepayment(Guid loanId, [FromBody] CreateRepaymentRequestDto request)
        {
            // Validate request
            var validator = new RepaymentValidator.CreateRepaymentValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return ApiResponseHelper.BadRequestResponse("Invalid request data");
            }

            var userId = GetCurrentUserId();
            var result = await repaymentService.CreateRepaymentAsync(loanId, request, userId);

            if (!result.IsSuccess)
            {
                return ApiResponseHelper.BadRequestResponse(result.ErrorMessage ?? "Repayment error");
            }

            return ApiResponseHelper.CreatedResponse(result.Data, "Repayment created successfully", nameof(GetRepayments), new { loanId });
        }

        [HttpPut("{repaymentId}")]
        public async Task<IActionResult> UpdateRepayment(Guid loanId, Guid repaymentId, [FromBody] UpdateRepaymentRequestDto request)
        {
            // Validate request
            var validator = new RepaymentValidator.UpdateRepaymentValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return ApiResponseHelper.BadRequestResponse("Invalid request data");
            }

            var userId = GetCurrentUserId();
            var result = await repaymentService.UpdateRepaymentAsync(loanId, repaymentId, request, userId);

            if (!result.IsSuccess)
            {
                return ApiResponseHelper.BadRequestResponse(result.ErrorMessage ?? "Repayment error");
            }

            return ApiResponseHelper.OkResponse(result.Data, "Repayment updated successfully");
        }

        [HttpDelete("{repaymentId}")]
        public async Task<IActionResult> DeleteRepayment(Guid loanId, Guid repaymentId)
        {
            var userId = GetCurrentUserId();
            await repaymentService.DeleteRepaymentAsync(loanId, repaymentId, userId);
            return ApiResponseHelper.NoContentResponse("Repayment deleted successfully");
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetRepaymentSummary([FromQuery] string role = "all")
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await repaymentService.GetRepaymentSummaryAsync(role, userId);
                return ApiResponseHelper.OkResponse(result, "Repayment summary retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return ApiResponseHelper.BadRequestResponse(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponseHelper.InternalServerErrorResponse("An unexpected error occurred.");
            }
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
