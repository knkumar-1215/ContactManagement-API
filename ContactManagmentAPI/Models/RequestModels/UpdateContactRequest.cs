using System.ComponentModel.DataAnnotations;

namespace ContactManagmentAPI.Models.RequestModels;

public class UpdateContactRequest
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; }

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; }
}
