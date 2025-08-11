using ChatbotPlatform.API.Models.Entities;

namespace ChatbotPlatform.API.Models.DTOs.Auth
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public UserRole Role { get; set; }
        public UserName Name { get; set; } = new();
        public string? PhoneNumber { get; set; }
        public string? ProfilePic { get; set; }
        public string? Department { get; set; }
        public UserStatus Status { get; set; }
        public string Timezone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
