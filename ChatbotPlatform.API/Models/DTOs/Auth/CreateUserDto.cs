using System.ComponentModel.DataAnnotations;
using ChatbotPlatform.API.Models.Entities;

namespace ChatbotPlatform.API.Models.DTOs.Auth;

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    //[Required]
    public string? CompanyId { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Timezone { get; set; }

    // Internal field - set by controller
    public string? CreatedBy { get; set; }
}