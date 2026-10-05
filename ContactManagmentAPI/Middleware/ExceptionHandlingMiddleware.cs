using ContactManagmentAPI.Models.ResponseModels;

namespace ContactManagmentAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);  // run the next middleware
        }
        catch (Exception ex)
        {
            // log full details
            _logger.LogError(ex,
                "Unhandled exception on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            // return consistent error response
            // never expose ex.Message to client
            await HandleExceptionAsync(context);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(
      new List<string> { "An unexpected error occurred" },
      "Internal server error",
      context.TraceIdentifier);


        await context.Response.WriteAsJsonAsync(response);
    }
}
