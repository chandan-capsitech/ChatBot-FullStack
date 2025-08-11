using MongoDB.Bson.Serialization.Attributes;

namespace ChatbotPlatform.API.Models.Entities
{
    public class User : BaseEntity
    {
        [BsonElement("companyId")]
        public string? CompanyId { get; set; }

        [BsonElement("role")]
        public UserRole Role { get; set; }

        [BsonElement("name")]
        public UserName Name { get; set; } = new();

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("profilePic")]
        public string? ProfilePic { get; set; }

        [BsonElement("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [BsonElement("department")]
        public string? Department { get; set; }

        [BsonElement("dob")]
        public DateTime? DateOfBirth { get; set; }

        [BsonElement("status")]
        public UserStatus Status { get; set; } = UserStatus.Active;

        [BsonElement("timezone")]
        public string Timezone { get; set; } = "UTC";

        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }
    }

    public class UserName
    {
        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }

    public enum UserRole
    {
        SuperAdmin = 0,
        Admin = 1,
        Employee = 2
    }
    public enum UserStatus
    {
        Active = 0,
        Inactive = 1,
        Suspended = 2
    }
}