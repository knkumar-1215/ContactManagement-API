using System.ComponentModel.DataAnnotations;

namespace ContactManagmentAPI.Models.RequestModels;

public class AuthenticationRequest
{
    [Required]
    public string UserName { get; set; }

    [Required]
    public string Password { get; set; }

}
