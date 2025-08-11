using ChatbotPlatform.API.Models.Entities;

namespace ChatbotPlatform.API.Models.DTOs.Auth;

public class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? ProfilePic { get; set; }
    public string? Timezone { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
}

public class UpdateUserStatusDto
{
    public UserStatus Status { get; set; }
}