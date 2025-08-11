using ChatbotPlatform.API.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace ChatbotPlatform.API.Models.DTOs.Auth
{
    public class RegisterDto
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

        [Required]
        public string? CompanyId { get; set; }


        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
