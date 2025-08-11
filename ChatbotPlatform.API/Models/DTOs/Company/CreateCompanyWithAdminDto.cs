using ChatbotPlatform.API.Models.DTOs.Auth;
using System.ComponentModel.DataAnnotations;

namespace ChatbotPlatform.API.Models.DTOs.Company;

public class CreateCompanyWithAdminDto
{
    [Required]
    public CreateCompanyDto CompanyDetails { get; set; } = new();

    [Required]
    public CreateAdminDto AdminDetails { get; set; } = new();
}

public class CreateAdminDto
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

    public string? PhoneNumber { get; set; }
    public string? Timezone { get; set; }
}

public class CompanyCreationResponseDto
{
    public CompanyDto Company { get; set; } = new();
    public UserDto Admin { get; set; } = new();
}