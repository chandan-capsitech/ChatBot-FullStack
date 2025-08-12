namespace ChatbotPlatform.API.Models.DTOs.FAQ
{
    public class FAQStatsDto
    {
        public string CompanyId { get; set; } = string.Empty;
        public int CurrentFAQCount { get; set; }
        public int MaxFAQsAllowed { get; set; }
        public string Subscription { get; set; } = string.Empty;
        public int RemainingFAQs { get; set; }
        public double UsagePercentage { get; set; }
    }
}