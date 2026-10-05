using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace ContactManagmentAPI.Filters
{
    public class LoggingFilter : IActionFilter
    {
        private readonly ILogger<LoggingFilter> _logger;
        private Stopwatch? _stopwatch;

        public LoggingFilter(ILogger<LoggingFilter> logger)
        {
            _logger = logger;
        }

      
        public void OnActionExecuting(ActionExecutingContext context)
        {
            
            _stopwatch = Stopwatch.StartNew();

            var userId = context.HttpContext.User
  .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
  ?.Value ?? "anonymous";

            _logger.LogInformation(
                "{Method} {Path} called by {UserId} at {Timestamp}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                userId,
                DateTime.UtcNow);
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch?.Stop();

            if (_stopwatch?.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "{Method} {Path} took {ElapsedMs}ms - exceeds 1 second threshold",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    _stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "{Method} {Path} completed in {ElapsedMs}ms",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    _stopwatch.ElapsedMilliseconds);
            }
        }

       
    }
}
