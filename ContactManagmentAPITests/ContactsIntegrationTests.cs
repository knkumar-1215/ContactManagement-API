using ContactManagmentAPI.Models.ResponseModels;
using DataAccessLibrary.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContactManagmentAPITests;

public class ContactsIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ContactsIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
   
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true  
    };
    // Helper — gets JWT token for a user
    private async Task<string> GetTokenAsync(
    string userName, string password)
    {
        var loginRequest = new
        {
            UserName = userName,
            Password = password
        };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/Authentication/token", loginRequest);

        // DEBUG — print the raw JSON to see what comes back
        var jsonString = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Token response: {jsonString}");
        Console.WriteLine($"Status: {response.StatusCode}");

        var content = JsonSerializer.Deserialize<
            ApiResponse<AuthResponse>>(jsonString, JsonOptions);

        Console.WriteLine($"IsSuccess: {content?.IsSuccess}");
        Console.WriteLine($"Data: {content?.Data}");
        Console.WriteLine($"Token: {content?.Data?.Token}");

        return content!.Data!.Token;
    }

    // Helper — sets token on client
    private async Task AuthenticateAsAdminAsync()
    {
        var token = await GetTokenAsync("Naga", "Test1234");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);
    }

    private async Task AuthenticateAsUserAsync()
    {
        var token = await GetTokenAsync("Padhu", "Test1234");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);
    }

    // Remove auth header
    private void RemoveAuthentication()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }
    [Fact]
    public async Task GetContacts_WithoutToken_Returns401()
    {
        // Act — no token attached
        var response = await _client.GetAsync("/api/v1/Contacts");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetContacts_WithValidToken_Returns200()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await _client.GetAsync(
            "/api/v1/Contacts?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResponse<ContactModel>>>();

        Assert.NotNull(content);
        Assert.True(content.IsSuccess);
    }

    [Fact]
    public async Task CreateContact_WithUserToken_Returns403()
    {
        // Arrange — User role cannot create
        await AuthenticateAsUserAsync();

        var newContact = new
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            Phone = "1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
      "/api/v1/Contacts", newContact);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateContact_WithAdminToken_Returns201()
    {
        // Arrange — Admin can create
        await AuthenticateAsAdminAsync();

        var newContact = new
        {
            FirstName = "Integration",
            LastName = "Test",
            Email = $"integration{Guid.NewGuid()}@test.com",
            Phone = "1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/Contacts", newContact);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<ApiResponse<ContactModel>>();

        Assert.NotNull(content);
        Assert.True(content.IsSuccess);
        Assert.Equal("Integration", content.Data.FirstName);
    }

    [Fact]
    public async Task GetContactById_InvalidId_Returns404()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act — use ID that does not exist
        var response = await _client.GetAsync(
            "/api/v1/Contacts/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<ApiResponse<ContactModel>>();

        Assert.NotNull(content);
        Assert.False(content.IsSuccess);
    }

    [Fact]
    public async Task CreateContact_WithInvalidData_Returns400()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Missing required fields
        var invalidContact = new
        {
            FirstName = "",   // required — empty fails
            LastName = "",   // required — empty fails
            Email = "notanemail",  // invalid format
            Phone = "abc"          // not numeric
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/Contacts", invalidContact);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateContact_DuplicateEmail_Returns409()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var email = $"duplicate{Guid.NewGuid()}@test.com";

        var contact = new
        {
            FirstName = "First",
            LastName = "Contact",
            Email = email,
            Phone = "1234567890"
        };

        // Create first time — should succeed
        await _client.PostAsJsonAsync("/api/v1/Contacts", contact);

        // Act — create same email again
        var response = await _client.PostAsJsonAsync(
            "/api/v1/Contacts", contact);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteContact_WithUserToken_Returns403()
    {
        // Arrange — User cannot delete
        await AuthenticateAsUserAsync();

        // Act
        var response = await _client
            .DeleteAsync("/api/v1/Contacts/1");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

 
}
