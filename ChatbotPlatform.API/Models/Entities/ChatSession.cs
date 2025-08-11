using MongoDB.Bson.Serialization.Attributes;

namespace ChatbotPlatform.API.Models.Entities
{
    public class ChatSession : BaseEntity
    {
        [BsonElement("sessionId")]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("companyId")]
        public string CompanyId { get; set; } = string.Empty;

        [BsonElement("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [BsonElement("assignedEmployeeId")]
        public string? AssignedEmployeeId { get; set; }

        [BsonElement("status")]
        public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Active;

        [BsonElement("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        [BsonElement("startedAt")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("endedAt")]
        public DateTime? EndedAt { get; set; }
    }

    public class ChatMessage
    {
        [BsonElement("sender")]
        public MessageSender Sender { get; set; }

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("messageType")]
        public MessageType MessageType { get; set; } = MessageType.Text;
    }

    public enum ChatSessionStatus
    {
        Active = 0,
        Pending = 1,
        Closed = 2,
    }

    public enum MessageSender
    {
        Customer = 0,
        Bot = 1,
        Employee = 2,
    }

    public enum MessageType
    {
        Text = 0,
        Image = 1,
        File = 2
    }
}
