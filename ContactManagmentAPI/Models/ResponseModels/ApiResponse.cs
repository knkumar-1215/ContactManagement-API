

namespace ContactManagmentAPI.Models.ResponseModels;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public string TraceId { get; set; }

    // Add these two static methods
    public static ApiResponse<T> Success(
        T data, string message, string traceId)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            TraceId = traceId
        };
    }

    public static ApiResponse<T> Failure(
        List<string> errors, string message, string traceId)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errors,
            Message = message,
            TraceId = traceId
        };
    }
}
