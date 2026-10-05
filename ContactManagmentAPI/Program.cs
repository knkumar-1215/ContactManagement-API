using ContactManagmentAPI.Extensions;
using ContactManagmentAPI.Filters;
using ContactManagmentAPI.Middleware;
using ContactManagmentAPI.Models.ResponseModels;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);



builder.AddStandardServices();
builder.AddAuthServices();
builder.AddHealthCheckServices();
builder.AddCustomServices();


var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 403
        && !context.Response.HasStarted)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Failure(
                new List<string> {
                    "You do not have permission. Admin role required."
                },
                "Forbidden",
                context.TraceIdentifier));
    }

    if (context.Response.StatusCode == 401
        && !context.Response.HasStarted)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Failure(
                new List<string> {
                    "Authentication required. Please provide a valid token."
                },
                "Unauthorized",
                context.TraceIdentifier));
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapHealthChecks("/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false  // skip all checks
                                // just confirms app running
    })
    .AllowAnonymous();

// Readiness — no auth, tests dependencies
app.MapHealthChecks("/health/ready",
    new HealthCheckOptions
    {
        Predicate = check =>
            check.Tags.Contains("ready")
    })
    .AllowAnonymous();

// Full health — Admin only
app.MapHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter =
            UIResponseWriter.WriteHealthCheckUIResponse
    })
    .RequireAuthorization(policy =>
        policy.RequireRole("Admin"));

// Health UI dashboard
app.MapHealthChecksUI()
    .AllowAnonymous();



app.Run();

public partial class Program { }