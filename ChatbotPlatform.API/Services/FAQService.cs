using AutoMapper;
using ChatbotPlatform.API.Data;
using ChatbotPlatform.API.Models.DTOs.FAQ;
using ChatbotPlatform.API.Models.Entities;
using MongoDB.Driver;
using System.ComponentModel.Design;

namespace ChatbotPlatform.API.Services;

public class FAQService
{
    private readonly MongoDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<FAQService> _logger;

    public FAQService(MongoDbContext context, IMapper mapper, ILogger<FAQService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<FAQDto>> GetByCompanyAsync(string companyId)
    {
        var faqs = await _context.FAQs.Find(f => f.CompanyId == companyId).SortBy(f => f.Depth).ThenBy(f => f.CreatedAt).ToListAsync();
        return _mapper.Map<List<FAQDto>>(faqs);
    }

    public async Task<List<FAQDto>> GetTopLevelAsync(string companyId)
    {
        var faqs = await _context.FAQs.Find(f => f.CompanyId == companyId && f.Depth == 1).ToListAsync();
        return _mapper.Map<List<FAQDto>>(faqs);
    }

    public async Task<FAQDto> GetByIdAsync(string id)
    {
        var faq = await _context.FAQs.Find(f => f.Id == id).FirstOrDefaultAsync();

        if (faq == null)
        {
            throw new Exception("FAQ not found");
        }

        return _mapper.Map<FAQDto>(faq);
    }

    public async Task<FAQDto> CreateAsync(CreateFAQDto createFaqDto, string createdBy, string companyId)
    {
        // Get company to check FAQ limits
        var company = await _context.Companies.Find(c => c.Id == companyId).FirstOrDefaultAsync();

        if (company == null)
        {
            throw new InvalidOperationException("Companies not found");
        }

        //chech FAQ limit
        var currentFAQCount = await _context.FAQs.CountDocumentsAsync(f => f.CompanyId == companyId);
        if (currentFAQCount >= company.SubscriptionLimits.MaxFAQs)
        {
            throw new InvalidOperationException($"Cannot create more FAQs. Your {company.Subscription} subscription allows maximum {company.SubscriptionLimits.MaxFAQs} FAQs. Current: {currentFAQCount}");
        }

        var faq = new FAQ
        {
            CompanyId = companyId,
            Depth = createFaqDto.Depth,
            Question = createFaqDto.Question,
            Answer = createFaqDto.Answer,
            Options = MapCreateFAQDtoToFAQ(createFaqDto.Options, companyId, createdBy),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.FAQs.InsertOneAsync(faq);

        _logger.LogInformation("FAQ created by {CreatedBy} for company {CompanyId}: {Question}", createdBy, companyId, faq.Question);

        return _mapper.Map<FAQDto>(faq);
    }
    public async Task<FAQDto> UpdateAsync(string id, UpdateFAQDto updateFaqDto, string updatedBy, string companyId)
    {
        var existingFaq = await _context.FAQs.Find(f => f.Id == id).FirstOrDefaultAsync();

        if (existingFaq == null)
        {
            throw new Exception("FAQ not found");
        }

        existingFaq.Depth = updateFaqDto.Depth;
        existingFaq.Question = updateFaqDto.Question;
        existingFaq.Options = MapCreateFAQDtoToFAQ(updateFaqDto.Options, companyId, updatedBy);
        existingFaq.Answer = updateFaqDto.Answer;
        existingFaq.UpdatedBy = updatedBy;
        existingFaq.UpdatedAt = DateTime.UtcNow;

        await _context.FAQs.ReplaceOneAsync(f => f.Id == id, existingFaq);

        _logger.LogInformation("FAQ updated by {UpdatedBy} for company {CompanyId}: {Question}", updatedBy, companyId, existingFaq.Question);
        return _mapper.Map<FAQDto>(existingFaq);
    }

    public async Task DeleteAsync(string id, string companyId)
    {
        var result = await _context.FAQs.DeleteOneAsync(f => f.Id == id && f.CompanyId == companyId);
        if (result.DeletedCount == 0)
        {
            throw new Exception("FAQ not found");
        }
        _logger.LogInformation("FAQ deleted for company {CompanyId}: {Id}", companyId, id);
    }

    public async Task<List<FAQDto>> SearchAsync(string companyId, string searchTerm)
    {
        var filter = Builders<FAQ>.Filter.And(
            Builders<FAQ>.Filter.Eq(f => f.CompanyId, companyId),
            Builders<FAQ>.Filter.Or(
                Builders<FAQ>.Filter.Regex(f => f.Question, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<FAQ>.Filter.Regex(f => f.Answer, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            )
        );

        var faqs = await _context.FAQs.Find(filter).ToListAsync();
        return _mapper.Map<List<FAQDto>>(faqs);
    }

    public async Task<string> GetBotResponseAsync(string companyId, string userMessage)
    {
        var faqs = await _context.FAQs.Find(f => f.CompanyId == companyId).ToListAsync();

        var matchingFaq = faqs.FirstOrDefault(f =>
            f.Question.Contains(userMessage, StringComparison.OrdinalIgnoreCase) ||
            userMessage.Contains(f.Question, StringComparison.OrdinalIgnoreCase));

        return matchingFaq?.Answer ?? "I'm sorry, I don't have information about that. Would you like to speak with a human agent?";
    }


    private List<FAQ>? MapCreateFAQDtoToFAQ(List<CreateFAQDto>? options, string companyId, string createdBy)
    {
        if (options == null || !options.Any())
        {
            return new List<FAQ>();
        }

        return options.Select(option => new FAQ
        {
            CompanyId = companyId,
            Depth = option.Depth,
            Question = option.Question,
            Answer = option.Answer,
            Options = MapCreateFAQDtoToFAQ(option.Options, companyId, createdBy),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
    }
}