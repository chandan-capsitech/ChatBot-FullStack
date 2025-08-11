using ChatbotPlatform.API.Models.Entities;

namespace ChatbotPlatform.API.Utilities
{
    public class SubscriptionDefaults
    {
        public static SubscriptionLimits GetLimitsForSubscription(SubscriptionType subscription)
        {
            return subscription switch
            {
                SubscriptionType.Basic => new SubscriptionLimits
                {
                    MaxAdmins = 1,
                    MaxEmployees = 5,
                    MaxFAQs = 25,
                    MaxChatSessions = 50
                },
                SubscriptionType.Pro => new SubscriptionLimits
                {
                    MaxAdmins = 3,
                    MaxEmployees = 25,
                    MaxFAQs = 100,
                    MaxChatSessions = 500
                },
                SubscriptionType.Premium => new SubscriptionLimits
                {
                    MaxAdmins = 5,
                    MaxEmployees = 100,
                    MaxFAQs = 500,
                    MaxChatSessions = 2000
                },
                SubscriptionType.Enterprise => new SubscriptionLimits
                {
                    MaxAdmins = 10,
                    MaxEmployees = 500,
                    MaxFAQs = 2000,
                    MaxChatSessions = 10000
                },
                _ => new SubscriptionLimits()
            };
        }
    }
}