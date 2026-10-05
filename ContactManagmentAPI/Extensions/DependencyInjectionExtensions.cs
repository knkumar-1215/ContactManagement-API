using Asp.Versioning;
using ContactManagmentAPI.Filters;
using ContactManagmentAPI.Models.ResponseModels;
using DataAccessLibrary;
using DataAccessLibrary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace ContactManagmentAPI.Extensions;

public static class DependencyInjectionExtensions
{
    public static void AddStandardServices(
    this WebApplicationBuilder builder)
    {
        // Only call AddControllers once with options
        builder.Services.AddControllers(opts =>
        {
            opts.Filters.Add<LoggingFilter>();
        })
        .AddJsonOptions(opts =>
        {
            // Return PascalCase — matches C# models exactly
            opts.JsonSerializerOptions.PropertyNamingPolicy = null;
        });

        builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(opts =>
            {
                opts.SuppressModelStateInvalidFilter = true;
            });

        // Add this to handle 403 responses
        builder.Services.Configure<ApiBehaviorOptions>(opts =>
        {
            opts.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return new BadRequestObjectResult(
                    ApiResponse<object>.Failure(
                        errors,
                        "Validation failed",
                        context.HttpContext.TraceIdentifier));
            };
        });
        builder.Services.AddScoped<LoggingFilter>();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddApiVersioning(opts =>
        {
            opts.DefaultApiVersion = new ApiVersion(1, 0);
            opts.AssumeDefaultVersionWhenUnspecified = true;
            opts.ReportApiVersions = true;
        })
        .AddApiExplorer(opts =>
        {
            opts.GroupNameFormat = "'v'VVV";
            opts.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddMemoryCache();
        builder.AddRateLimitServices();
        builder.AddSwaggerServices();
    }

    private static void AddRateLimitServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            // Return HTTP 429 instead of HTTP 503
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Optional: Return a custom JSON message for 429 responses
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.Headers.RetryAfter = "60";

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Failure(
                        new List<string> {
                "Too many requests. Please try again later. " +
                "Retry after 60 seconds."
                        },
                        "Rate limit exceeded",
                        context.HttpContext.TraceIdentifier),
                    token);
            };

            // POLICY 1: "anonymous" → 10/min per IP
            options.AddPolicy("anonymous", httpContext =>
            {
                // Fallback to "localhost" if the IP cannot be determined
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "localhost";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress, // Separates counts per individual IP
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true
                    });
            });

            // POLICY 2: "authenticated" → 100/min per user
            options.AddPolicy("authenticated", httpContext =>
            {
                // Extract the user identity claim (usually Name Identifier or Subject)
                var username = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? httpContext.User.Identity?.Name
                               ?? "anonymous-fallback";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: username, // Separates counts per individual user account
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true
                    });
            });

            // POLICY 3: "auth-endpoint" → 5/min per IP (Best for Login/Register endpoints)
            options.AddPolicy("auth-endpoint", httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "localhost";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true
                    });
            });
        });
        }
    private static void AddSwaggerServices(this WebApplicationBuilder builder)
    {
        var securityScheme = new OpenApiSecurityScheme()
        {
            Name = "Authorization",
            Description = "JWT Authorization header info using bearer tokens",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        var securityRequirment = new OpenApiSecurityRequirement
        {
            {
                 new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "beareAuth"
                }
            },
                 new string[] {}
            }
        };
        builder.Services.AddSwaggerGen(opts =>
        {
            opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token. Example: eyJhbGci..."
            });

            opts.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

        });
    }

    public static void AddCustomServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ISqlDataAccess, SqlDataAccess>();
        builder.Services.AddTransient<IContactRepository, ContactRepository>();
    }

    public static void AddAuthServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(opts =>
        {
            opts.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration.GetValue<string>("Authentication:Issuer"),
                    ValidAudience = builder.Configuration.GetValue<string>("Authentication:Audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(
                        builder.Configuration.GetValue<string>("Authentication:SecretKey")))
                };
            });
    }

    public static void AddHealthCheckServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddSqlServer(
                builder.Configuration
                    .GetConnectionString("Default"),
                name: "SQL Server",
                tags: new[] { "db", "ready" });

        builder.Services
            .AddHealthChecksUI(opts =>
            {
                opts.AddHealthCheckEndpoint(
                    "Contact Manager API", "/health/ready");
                opts.SetEvaluationTimeInSeconds(60);
            })
            .AddInMemoryStorage();
    }
}
