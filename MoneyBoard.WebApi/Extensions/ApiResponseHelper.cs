using Microsoft.AspNetCore.Mvc;
using MoneyBoard.Application.DTOs;

namespace MoneyBoard.WebApi.Extensions
{
    public static class ApiResponseHelper
    {
        public static IActionResult OkResponse<T>(T data, string message = "Request processed successfully")
        {
            var response = ApiResponse<T>.SuccessResponse(data, message, 200);
            return new OkObjectResult(response);
        }

        public static IActionResult CreatedResponse<T>(T data, string message = "Resource created successfully", string actionName = null, object routeValues = null)
        {
            var response = ApiResponse<T>.SuccessResponse(data, message, 201);
            if (actionName != null && routeValues != null)
            {
                return new CreatedAtActionResult(actionName, null, routeValues, response);
            }
            return new ObjectResult(response) { StatusCode = 201 };
        }

        public static IActionResult NoContentResponse(string message = "No content")
        {
            var response = ApiResponse<object>.NoContentResponse(message, 204);
            return new NoContentResult();
        }

        public static IActionResult BadRequestResponse(string message = "Bad request")
        {
            var response = ApiResponse<object>.ErrorResponse(message, 400);
            return new BadRequestObjectResult(response);
        }

        public static IActionResult UnauthorizedResponse(string message = "Unauthorized")
        {
            var response = ApiResponse<object>.ErrorResponse(message, 401);
            return new UnauthorizedObjectResult(response);
        }

        public static IActionResult ForbiddenResponse(string message = "Forbidden")
        {
            var response = ApiResponse<object>.ErrorResponse(message, 403);
            return new ObjectResult(response) { StatusCode = 403 };
        }

        public static IActionResult NotFoundResponse(string message = "Not found")
        {
            var response = ApiResponse<object>.ErrorResponse(message, 404);
            return new NotFoundObjectResult(response);
        }

        public static IActionResult InternalServerErrorResponse(string message = "Internal server error")
        {
            var response = ApiResponse<object>.ErrorResponse(message, 500);
            return new ObjectResult(response) { StatusCode = 500 };
        }
    }
}