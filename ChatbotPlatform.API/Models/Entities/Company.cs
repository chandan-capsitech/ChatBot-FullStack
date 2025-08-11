using MongoDB.Bson.Serialization.Attributes;
using System.Net;

namespace ChatbotPlatform.API.Models.Entities
{
    public class Company : BaseEntity
    {
        [BsonElement("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [BsonElement("companyType")]
        public string CompanyType { get; set; } = string.Empty;

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = "superadmin@capsitech.com";

        [BsonElement("subscription")]
        public SubscriptionType Subscription { get; set; }

        [BsonElement("subscriptionLimits")]
        public SubscriptionLimits SubscriptionLimits { get; set; } = new();

        [BsonElement("employeeCount")]
        public int EmployeeCount { get; set; } = 0;

        [BsonElement("adminCount")]
        public int AdminCount { get; set; } = 0;

        [BsonElement("status")]
        public CompanyStatus Status { get; set; } = CompanyStatus.Active;

        [BsonElement("domains")]
        public List<string> Domains { get; set; } = new();

        [BsonElement("address")]
        public Address? Address { get; set; }

        [BsonElement("contactDetails")]
        public ContactDetails? ContactDetails { get; set; }

        [BsonElement("subscribed")]
        public bool Subscribed => Status == CompanyStatus.Active;
    }

    public class SubscriptionLimits
    {
        [BsonElement("maxAdmins")]
        public int MaxAdmins { get; set; } = 1;

        [BsonElement("maxEmployees")]
        public int MaxEmployees { get; set; } = 5;

        [BsonElement("maxFAQs")]
        public int MaxFAQs { get; set; } = 50;

        [BsonElement("maxChatSessions")]
        public int MaxChatSessions { get; set; } = 100;
    }

    public class Address
    {
        [BsonElement("addressName")]
        public string AddressName { get; set; } = string.Empty;

        [BsonElement("addressType")]
        public AddressType AddressType { get; set; }

        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [BsonElement("street")]
        public string? Street { get; set; }

        [BsonElement("landmark")]
        public string? Landmark { get; set; }

        [BsonElement("pinCode")]
        public string PinCode { get; set; } = string.Empty;

        [BsonElement("district")]
        public string District { get; set; } = string.Empty;

        [BsonElement("state")]
        public string State { get; set; } = string.Empty;

        [BsonElement("country")]
        public string Country { get; set; } = string.Empty;
    }

    public class ContactDetails
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("designation")]
        public string Designation { get; set; } = string.Empty;

        [BsonElement("primaryEmail")]
        public string PrimaryEmail { get; set; } = string.Empty;

        [BsonElement("supportPhone")]
        public string SupportPhone { get; set; } = string.Empty;

        [BsonElement("cc")]
        public string? CC { get; set; }
    }

    public enum AddressType
    {
        Office = 0,
        Institute = 1,
        Home = 2
    }

    public enum SubscriptionType
    {
        Basic = 0,
        Pro = 1,
        Premium = 2,
        Enterprise = 3,
    }

    public enum CompanyStatus
    {
        Active = 0,
        Inactive = 1,
        Suspended = 2
    }
}