using System.ComponentModel.DataAnnotations;

namespace ContactManagmentAPI.Models.RequestModels;

public class RegistrationRequest
{
    [Required]
    public string UserName { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; }

    [Required]
    public string Role { get; set; }
}
