using ChatbotPlatform.API.Models.Entities;

namespace ChatbotPlatform.API.Models.DTOs.Chat
{
    public class ChatSessionDto
    {
        public string Id { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string? AssignedEmployeeId { get; set; }
        public ChatSessionStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public class ChatMessageDto
    {
        public MessageSender Sender { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public MessageType MessageType { get; set; }
    }
}