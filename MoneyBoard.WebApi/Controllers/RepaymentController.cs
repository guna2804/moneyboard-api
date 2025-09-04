using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Application.Validators;

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
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRepayment(Guid loanId, [FromBody] CreateRepaymentRequestDto request)
        {
            // Validate request
            var validator = new RepaymentValidator.CreateRepaymentValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { code = "VALIDATION_ERROR", message = "Invalid request data", details = validationResult.Errors });
            }

            var userId = GetCurrentUserId();
            var result = await repaymentService.CreateRepaymentAsync(loanId, request, userId);
            return CreatedAtAction(nameof(GetRepayments), new { loanId }, result);
        }

        [HttpPut("{repaymentId}")]
        public async Task<IActionResult> UpdateRepayment(Guid loanId, Guid repaymentId, [FromBody] UpdateRepaymentRequestDto request)
        {
            // Validate request
            var validator = new RepaymentValidator.UpdateRepaymentValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { code = "VALIDATION_ERROR", message = "Invalid request data", details = validationResult.Errors });
            }

            var userId = GetCurrentUserId();
            var result = await repaymentService.UpdateRepaymentAsync(loanId, repaymentId, request, userId);
            return Ok(result);
        }

        [HttpDelete("{repaymentId}")]
        public async Task<IActionResult> DeleteRepayment(Guid loanId, Guid repaymentId)
        {
            var userId = GetCurrentUserId();
            await repaymentService.DeleteRepaymentAsync(loanId, repaymentId, userId);
            return NoContent();
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
