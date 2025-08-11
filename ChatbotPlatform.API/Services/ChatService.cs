using AutoMapper;
using ChatbotPlatform.API.Data;
using ChatbotPlatform.API.Models.DTOs.Chat;
using ChatbotPlatform.API.Models.Entities;
using MongoDB.Driver;

namespace ChatbotPlatform.API.Services;

public class ChatService
{
    private readonly MongoDbContext _context;
    private readonly FAQService _faqService;
    private readonly IMapper _mapper;

    public ChatService(MongoDbContext context, FAQService faqService, IMapper mapper)
    {
        _context = context;
        _faqService = faqService;
        _mapper = mapper;
    }

    public async Task<ChatSession> StartSessionAsync(string companyId, string customerId)
    {
        var session = new ChatSession
        {
            CompanyId = companyId,
            CustomerId = customerId,
            Status = ChatSessionStatus.Active,
            StartedAt = DateTime.UtcNow,
            Messages = new List<ChatMessage>()
        };

        await _context.ChatSessions.InsertOneAsync(session);
        return session;
    }

    public async Task<ChatMessageDto> HandleCustomerMessageAsync(string sessionId, string message)
    {
        var session = await _context.ChatSessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

        if (session == null)
        {
            throw new Exception("Chat session not found");
        }

        var customerMessage = new ChatMessage
        {
            Sender = MessageSender.Customer,
            Message = message,
            Timestamp = DateTime.UtcNow,
            MessageType = MessageType.Text
        };

        session.Messages.Add(customerMessage);

        var botResponse = await _faqService.GetBotResponseAsync(session.CompanyId, message);
        var botMessage = new ChatMessage
        {
            Sender = MessageSender.Bot,
            Message = botResponse,
            Timestamp = DateTime.UtcNow,
            MessageType = MessageType.Text
        };

        session.Messages.Add(botMessage);
        session.UpdatedAt = DateTime.UtcNow;

        await _context.ChatSessions.ReplaceOneAsync(s => s.Id == session.Id, session);

        return _mapper.Map<ChatMessageDto>(botMessage);
    }

    public async Task RequestHumanAsync(string sessionId)
    {
        var session = await _context.ChatSessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

        if (session == null)
        {
            throw new Exception("Chat session not found");
        }

        session.Status = ChatSessionStatus.Pending;
        session.UpdatedAt = DateTime.UtcNow;

        var systemMessage = new ChatMessage
        {
            Sender = MessageSender.Bot,
            Message = "I'm connecting you with a human agent. Please wait a moment...",
            Timestamp = DateTime.UtcNow,
            MessageType = MessageType.Text
        };

        session.Messages.Add(systemMessage);
        await _context.ChatSessions.ReplaceOneAsync(s => s.Id == session.Id, session);
    }

    public async Task AssignAgentAsync(string sessionId, string employeeId)
    {
        var session = await _context.ChatSessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

        if (session == null)
        {
            throw new Exception("Chat session not found");
        }

        session.AssignedEmployeeId = employeeId;
        session.Status = ChatSessionStatus.Active;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.ChatSessions.ReplaceOneAsync(s => s.Id == session.Id, session);
    }

    public async Task SaveAgentMessageAsync(string sessionId, string employeeId, string message)
    {
        var session = await _context.ChatSessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

        if (session == null)
        {
            throw new Exception("Chat session not found");
        }

        var agentMessage = new ChatMessage
        {
            Sender = MessageSender.Employee,
            Message = message,
            Timestamp = DateTime.UtcNow,
            MessageType = MessageType.Text
        };

        session.Messages.Add(agentMessage);
        session.UpdatedAt = DateTime.UtcNow;

        await _context.ChatSessions.ReplaceOneAsync(s => s.Id == session.Id, session);
    }

    public async Task<List<ChatSessionDto>> GetActiveSessionsAsync(string companyId)
    {
        var sessions = await _context.ChatSessions.Find(s => s.CompanyId == companyId && s.Status == ChatSessionStatus.Active).ToListAsync();
        return _mapper.Map<List<ChatSessionDto>>(sessions);
    }

    public async Task<List<ChatSessionDto>> GetSessionsByEmployeeAsync(string employeeId)
    {
        var sessions = await _context.ChatSessions.Find(s => s.AssignedEmployeeId == employeeId).SortByDescending(s => s.StartedAt).ToListAsync();
        return _mapper.Map<List<ChatSessionDto>>(sessions);
    }

    public async Task<ChatSessionDto> GetSessionAsync(string sessionId)
    {
        var session = await _context.ChatSessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

        if (session == null)
        {
            throw new Exception("Chat session not found");
        }

        return _mapper.Map<ChatSessionDto>(session);
    }

    public async Task CloseSessionAsync(string sessionId)
    {
        var session = await _context.ChatSessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

        if (session == null)
        {
            throw new Exception("Chat session not found");
        }

        session.Status = ChatSessionStatus.Closed;
        session.EndedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.ChatSessions.ReplaceOneAsync(s => s.Id == session.Id, session);
    }
}