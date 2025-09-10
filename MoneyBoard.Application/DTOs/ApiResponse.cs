using System.Text.Json.Serialization;

namespace MoneyBoard.Application.DTOs
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        public ApiResponse(bool success, string message, T? data, int statusCode)
        {
            Success = success;
            Message = message;
            Data = data;
            StatusCode = statusCode;
        }

        // Factory methods for common responses
        public static ApiResponse<T> SuccessResponse(T data, string message = "Request processed successfully", int statusCode = 200)
        {
            return new ApiResponse<T>(true, message, data, statusCode);
        }

        public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400)
        {
            return new ApiResponse<T>(false, message, default, statusCode);
        }

        public static ApiResponse<object> NoContentResponse(string message = "No content", int statusCode = 204)
        {
            return new ApiResponse<object>(true, message, null, statusCode);
        }
    }
}