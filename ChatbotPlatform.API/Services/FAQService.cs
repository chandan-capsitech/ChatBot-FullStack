using AutoMapper;
using ChatbotPlatform.API.Data;
using ChatbotPlatform.API.Models.DTOs.FAQ;
using ChatbotPlatform.API.Models.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;

namespace ChatbotPlatform.API.Services;

public class FAQService
{
    private readonly MongoDbContext _context;
    private readonly IMapper _mapper;

    public FAQService(MongoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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

        if (faq is null)
        {
            throw new Exception("FAQ not found");
        }

        return _mapper.Map<FAQDto>(faq);
    }

    public async Task<FAQDto> CreateAsync(CreateFAQDto createFaqDto, string createdBy, string companyId)
    {
        // Get company to check FAQ limits
        var company = await _context.Companies.Find(c => c.Id == companyId).FirstOrDefaultAsync();

        if (company is null)
        {
            throw new InvalidOperationException("Company not found");
        }

        // Check current FAQ count against subscription limit
        var currentFaqCount = await _context.FAQs.CountDocumentsAsync(f => f.CompanyId == companyId);

        if (currentFaqCount >= company.SubscriptionLimits.MaxFAQs)
        {
            throw new InvalidOperationException($"Can not create more FAQs. Your {company.Subscription} subscription allows "
                + $"maximum {company.SubscriptionLimits.MaxFAQs}, current FAQs: {currentFaqCount}");
        }

        var faq = new FAQ
        {
            CompanyId = companyId,
            Depth = createFaqDto.Depth,
            Question = createFaqDto.Question,
            Answer = createFaqDto.Answer,
            Options = MapCreateFAQDtoToFAQ(createFaqDto.Options, companyId, createdBy),
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _context.FAQs.InsertOneAsync(faq);
        return _mapper.Map<FAQDto>(faq);
    }

    public async Task<FAQDto> UpdateAsync(string id, UpdateFAQDto updateFaqDto, string updatedBy, string companyId)
    {
        // Ensure FAQ belongs to the requesting company
        var existingFaq = await _context.FAQs.Find(f => f.Id == id && f.CompanyId == companyId).FirstOrDefaultAsync();

        if (existingFaq == null)
        {
            throw new Exception("FAQ not found or access denied for this company");
        }

        existingFaq.Depth = updateFaqDto.Depth;
        existingFaq.Question = updateFaqDto.Question;
        existingFaq.Answer = updateFaqDto.Answer;
        existingFaq.Options = MapCreateFAQDtoToFAQ(updateFaqDto.Options, companyId, updatedBy);
        existingFaq.UpdatedBy = updatedBy;
        existingFaq.UpdatedAt = DateTime.UtcNow;

        await _context.FAQs.ReplaceOneAsync(f => f.Id == id, existingFaq);

        return _mapper.Map<FAQDto>(existingFaq);
    }

    public async Task DeleteAsync(string id, string companyId)
    {
        // Ensure FAQ belongs to the requesting company
        var result = await _context.FAQs.DeleteOneAsync(f => f.Id == id && f.CompanyId == companyId);

        if (result.DeletedCount == 0)
        {
            throw new Exception("FAQ not found or access denied");
        }
    }


    // optional use
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
        var matchingFaq = await _context.FAQs.Find(
            f => f.CompanyId == companyId && (
            f.Question.ToLower().Contains(userMessage.ToLower()) ||
            userMessage.ToLower().Contains(f.Question.ToLower()))
        ).FirstOrDefaultAsync();

        return matchingFaq?.Answer ?? "I'm sorry, I don't have information about that. Would you like to speak with a human agent?";
    }


    // FAQ statistics for company
    public async Task<FAQStatsDto> GetFAQStatsAsync(string companyId)
    {
        var company = await _context.Companies.Find(c => c.Id == companyId).FirstOrDefaultAsync();
        if (company == null)
        {
            throw new Exception("Company not found");
        }

        var currentCount = await _context.FAQs.CountDocumentsAsync(f => f.CompanyId == companyId);

        return new FAQStatsDto
        {
            CompanyId = companyId,
            CurrentFAQCount = (int)currentCount,
            MaxFAQsAllowed = company.SubscriptionLimits.MaxFAQs,
            Subscription = company.Subscription.ToString(),
            RemainingFAQs = company.SubscriptionLimits.MaxFAQs - (int)currentCount,
            UsagePercentage = company.SubscriptionLimits.MaxFAQs > 0
                ? Math.Round(((double)currentCount / company.SubscriptionLimits.MaxFAQs) * 100)
                : 0
        };
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
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
    }
}