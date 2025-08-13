using ChatbotPlatform.API.Models.Entities;
namespace ChatbotPlatform.API.Models.DTOs.Company
{
    public class UpdateCompanyDto
    {
        public string? CompanyName { get; set; }
        public string? CompanyType { get; set; }
        public SubscriptionType? Subscription { get; set; }
        public List<string>? Domains { get; set; }
        public Address? Address { get; set; }
        public ContactDetails? ContactDetails { get; set; }
        public int? EmployeeCount { get; set; }
        public CompanyStatus? Status { get; set; }
    }
}