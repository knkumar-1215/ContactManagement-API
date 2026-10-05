using Asp.Versioning;
using ContactManagmentAPI.Models.RequestModels;
using ContactManagmentAPI.Models.ResponseModels;
using DataAccessLibrary.Interfaces;
using DataAccessLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;



namespace ContactManagmentAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
public class ContactsController : ControllerBase
{
    private readonly IContactRepository _contactRepository;
    private readonly ILogger<ContactsController> _logger;
    private readonly IMemoryCache _memoryCache;
    private const string AdminListCacheKey = "contacts-Admin";
    private const string UserListCacheKey = "contacts-User";
    private string GetItemCacheKey(int id, string role)
    => $"contact-{id}-{role}";

    private void InvalidateListCache()
    {
        _memoryCache.Remove(AdminListCacheKey);
        _memoryCache.Remove(UserListCacheKey);
    }

    private void InvalidateItemCache(int id)
    {
        _memoryCache.Remove(GetItemCacheKey(id, "Admin"));
        _memoryCache.Remove(GetItemCacheKey(id, "User"));
    }

    public ContactsController(IContactRepository contactRepository, ILogger<ContactsController> logger, IMemoryCache memoryCache)
    {
        _contactRepository = contactRepository;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Get all contacts with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of contacts</returns>
    /// <response code="200">Contacts retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>

    [HttpGet]
    [Authorize]
    [EnableRateLimiting("authenticated")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ContactModel>>>>
     GetContacts(
         [FromQuery] int page = 1,
         [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;

            var userRole = User.FindFirst(ClaimTypes.Role)
                ?.Value ?? "anonymous";
            var cacheKey = $"contacts-{userRole}";

            // Check cache — works same for hit or miss
            if (!_memoryCache.TryGetValue(
                cacheKey, out List<ContactModel> allContacts))
            {
                _logger.LogInformation(
                    "Cache miss for key {CacheKey}", cacheKey);

                allContacts = await Task.Run(() =>
                    _contactRepository.GetAllContacts());

                _memoryCache.Set(cacheKey, allContacts,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromSeconds(60),
                        SlidingExpiration =
                            TimeSpan.FromSeconds(30)
                    });
            }
            else
            {
                _logger.LogInformation(
                    "Cache hit for key {CacheKey}", cacheKey);
            }

            // Pagination always runs — cache hit or miss
            var totalCount = allContacts.Count();
            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            var pagedData = allContacts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedResponse = new PagedResponse<ContactModel>
            {
                Data = pagedData,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(ApiResponse<PagedResponse<ContactModel>>.Success(
                pagedResponse,
                "Contacts retrieved successfully",
                HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contacts");

            return StatusCode(500, ApiResponse<object>.Failure(
                new List<string> { "An unexpected error occurred" },
                "Contact retrieval failed",
                HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ContactModel>>>
     GetContactByID(int id)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)
            ?.Value ?? "anonymous";
        var cacheKey = $"contact-{id}-{userRole}";

        if (!_memoryCache.TryGetValue(
            cacheKey, out ContactModel contact))
        {
            _logger.LogInformation(
                "Cache miss for key {CacheKey}", cacheKey);

            contact = await Task.Run(() =>
                _contactRepository.GetContactById(id));

            if (contact is null)
            {
                return NotFound(ApiResponse<ContactModel>.Failure(
                    new List<string> {
                    $"Contact with ID {id} was not found" },
                    "Not found",
                    HttpContext.TraceIdentifier));
            }

            _memoryCache.Set(cacheKey, contact,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(60)
                });
        }
        else
        {
            _logger.LogInformation(
                "Cache hit for key {CacheKey}", cacheKey);
        }

        return Ok(ApiResponse<ContactModel>.Success(
            contact,
            "Contact retrieved successfully",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("lastName")]
    [Authorize]
    public async Task<ActionResult<PagedResponse<ContactModel>>> GetContactByLastName([FromQuery] string lastName, [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation(
     "GetContactByLastName called for ID {LastName}", lastName);

            var allContactswithLastName = await Task.Run(() =>
                _contactRepository.GetContactsByLastName(lastName));

            if (allContactswithLastName is null)
            {
                return NotFound(ApiResponse<ContactModel>.Failure(
                    new List<string> { $"Contact with LastName {lastName} was not found" },
                    "Not found",
                    HttpContext.TraceIdentifier));
            }

            var totalCount = allContactswithLastName.Count();
            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            var pagedData = allContactswithLastName
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedResponse = new PagedResponse<ContactModel>
            {
                Data = pagedData,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(ApiResponse<PagedResponse<ContactModel>>.Success(
                pagedResponse,
                "Contacts retrieved successfully",
                HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
           "Error retrieving contacts");

            return StatusCode(500,
                ApiResponse<object>.Failure(
                    new List<string> { "An unexpected error occurred" },
                    "Contact retrieval failed",
                    HttpContext.TraceIdentifier));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ContactModel>>> CreateContact(
    [FromBody] CreateContactRequest request)
    {
        try
        {
            // MOdel state is used to autovalidate the annotations on your createContactRequest class
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.Failure(
                    errors,
                    "Validation failed",
                    HttpContext.TraceIdentifier));
            }

            _logger.LogInformation("CreateContact called");

            var existingContact = await Task.Run(() =>
                _contactRepository.GetContactByEmail(request.Email));

            if (existingContact != null)
            {
                return StatusCode(409, ApiResponse<object>.Failure(
                    new List<string> { $"Email '{request.Email}' already exists" },
                    "Creation failed",
                    HttpContext.TraceIdentifier));
            }

            var contactModel = new ContactModel
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                CreatedDate = DateTime.UtcNow
            };

            await Task.Run(() =>
                _contactRepository.CreateContact(contactModel));

            InvalidateListCache();

            Response.Headers.Location =
                $"/api/v1/contacts/{contactModel.ID}";

            return StatusCode(201, ApiResponse<ContactModel>.Success(
                contactModel,
                "Contact created successfully",
                HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact");

            return StatusCode(500, ApiResponse<object>.Failure(
                new List<string> { "An unexpected error occurred" },
                "Contact creation failed",
                HttpContext.TraceIdentifier));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ContactModel>>> UpdateContact(int id,
   [FromBody] UpdateContactRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.Failure(
                    errors,
                    "Validation failed",
                    HttpContext.TraceIdentifier));
            }

            _logger.LogInformation("UpdateContact called");

            var output = await Task.Run(() =>
                    _contactRepository.GetContactById(id));

            if (output is null)
            {

                return StatusCode(404, ApiResponse<ContactModel>.Failure(
                        new List<string> { $"Contact with ID {id} was not found" },
                        "Update failed",
                        HttpContext.TraceIdentifier));

            }

            if (output.Email != request.Email)
            {
                var existingContactWithEmail = await Task.Run(() =>
                   _contactRepository.GetContactByEmail(request.Email));

                if (existingContactWithEmail != null)
                {
                    return StatusCode(409, ApiResponse<object>.Failure(
                        new List<string> { $"Email '{request.Email}' already exists" },
                        "Update failed",
                        HttpContext.TraceIdentifier));
                }
            }

            output.ID = id;
            output.Email = request.Email;
            output.FirstName = request.FirstName;
            output.LastName = request.LastName;
            output.Phone = request.Phone;

            await Task.Run(() =>
                   _contactRepository.UpdateContact(output));

            InvalidateListCache();
            InvalidateItemCache(id);



            return StatusCode(200, ApiResponse<ContactModel>.Success(
                    output,
                    "Contact updated successfully",
                    HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {

            _logger.LogError(ex, "Error Updating contact");

            return StatusCode(500, ApiResponse<object>.Failure(
                new List<string> { "An unexpected error occurred" },
                "Contact Update failed",
                HttpContext.TraceIdentifier));
        }

    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]

    public async Task<ActionResult<ApiResponse<ContactModel>>> DeleteContact(int id)
    {
        try
        {
            _logger.LogInformation("DeleteContact called");

            var output = await Task.Run(() =>
                    _contactRepository.GetContactById(id));

            if (output is null)
            {

                return NotFound(ApiResponse<ContactModel>.Failure(
                    new List<string> { $"Contact with ID {id} was not found" },
                    "Not found",
                    HttpContext.TraceIdentifier));

            }

            await Task.Run(() =>
                  _contactRepository.DeleteContact(id));

            InvalidateListCache();
            InvalidateItemCache(id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Deleting contact");

            return StatusCode(500, ApiResponse<object>.Failure(
                new List<string> { "An unexpected error occurred" },
                "Contact deletion failed",
                HttpContext.TraceIdentifier));

        }
    }

}
