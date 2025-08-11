using ChatbotPlatform.API.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace ChatbotPlatform.API.Models.DTOs.Company
{
    public class CreateCompanyDto
    {
        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string CompanyType { get; set; } = string.Empty;

        public SubscriptionType Subscription { get; set; } = SubscriptionType.Basic;
        public List<string>? Domains { get; set; } = new();
        public Address? Address { get; set; }
        public ContactDetails? ContactDetails { get; set; }
    }
}