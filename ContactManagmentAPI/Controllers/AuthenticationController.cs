using Asp.Versioning;
using ContactManagmentAPI.Models.RequestModels;
using ContactManagmentAPI.Models.ResponseModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace ContactManagmentAPI.Controllers;

public record UserData(int Id, string FirstName, string LastName, string UserName, string Role);

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _config;
    
    public AuthenticationController(IConfiguration config)
    {
        _config = config;
    }

   

    // Static = shared across all requests (in-memory store)
    private static readonly List<UserData> _registeredUsers = new()
    {
        new UserData(1, "Naga", "K", "Naga", "Admin"),
        new UserData(2, "Padhu", "P", "Padhu", "User")
    };
    private static int _nextId = 3;


    [HttpPost("Registration")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-endpoint")]
    public ActionResult<ApiResponse<RegisterResponse>> Registration([FromBody] RegistrationRequest data)
    {
        try
        {
            bool userExists = _registeredUsers
    .Any(u => u.UserName.Equals(
        data.UserName,
        StringComparison.OrdinalIgnoreCase));
            if (userExists)
            {
                return Conflict(ApiResponse<RegisterResponse>.Failure(
         new List<string> { $"Username '{data.UserName}' already exists" },
         "Registration failed",
         HttpContext.TraceIdentifier));
            }

            // Create new user
            var newUser = new UserData(
                _nextId++,
                data.UserName!,
                string.Empty,
                data.UserName!,
                data.Role!);

            _registeredUsers.Add(newUser);

            var response = new RegisterResponse
            {
                Id = newUser.Id,
                UserName = newUser.UserName
            };

            return StatusCode(201, ApiResponse<RegisterResponse>.Success(
                    response,
                    "User registered successfully",
                    HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RegisterResponse>.Failure(
               new List<string> { "An unexpected error occurred" },
               "Registration failed",
               HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-endpoint")]
    public ActionResult<ApiResponse<AuthResponse>> Authenticate([FromBody] AuthenticationRequest data)
    {
        try
        {
            var user = ValidateCredentialsForAuthentication(data);

            if (user is null)
            {
                    return Unauthorized(ApiResponse<AuthResponse>.Failure(
                     new List<string> { "Invalid username or password" },
                     "Authentication failed",
                     HttpContext.TraceIdentifier));
            }

            string token = GenerateToken(user);

            AuthResponse response = new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                TokenType = "JWT"
            };

             return Ok(ApiResponse<AuthResponse>.Success(
             response,
             "User authenticated successfully",
             HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {

            return StatusCode(500, ApiResponse<AuthResponse>.Failure(
                   new List<string> { "An unexpected error occurred" },
                   "Authentication failed",
                   HttpContext.TraceIdentifier));
        }
    }
    
    private UserData? ValidateCredentialsForAuthentication(AuthenticationRequest data)
    {
        // NOTE: NOT PRODUCTION CODE
        if (string.IsNullOrWhiteSpace(data.Password) ||
            data.Password != "Test1234")
        {
            return null;
        }
        return _registeredUsers.FirstOrDefault(u =>
         u.UserName.Equals(data.UserName,
             StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateToken(UserData user)
    {
        var secretKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _config.GetValue<string>("Authentication:SecretKey")));

        var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new();
        claims.Add(new(JwtRegisteredClaimNames.Sub, user.Id.ToString()));
        claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.UserName));
        claims.Add(new(JwtRegisteredClaimNames.GivenName, user.FirstName));
        claims.Add(new(JwtRegisteredClaimNames.FamilyName, user.LastName));
        claims.Add(new Claim(ClaimTypes.Role, user.Role));


        var token = new JwtSecurityToken(
            _config.GetValue<string>("Authentication:Issuer"),
            _config.GetValue<string>("Authentication:Audience"),
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(60),
            signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
}
