using ChatbotPlatform.API.Models.Entities;

namespace ChatbotPlatform.API.Models.DTOs.Company
{
    public class CompanyDto
    {
        public string Id { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyType { get; set; } = string.Empty;
        public SubscriptionType Subscription { get; set; }
        public SubscriptionLimits SubscriptionLimits { get; set; } = new();
        public int EmployeeCount { get; set; }
        public int AdminCount { get; set; }
        public CompanyStatus Status { get; set; }
        public List<string> Domains { get; set; } = new();
        public Address? Address { get; set; }
        public ContactDetails? ContactDetails { get; set; }
        //public bool Subscribed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}