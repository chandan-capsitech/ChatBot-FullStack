using AutoMapper;
using ChatbotPlatform.API.Models.DTOs.Auth;
using ChatbotPlatform.API.Models.DTOs.Company;
using ChatbotPlatform.API.Models.DTOs.FAQ;
using ChatbotPlatform.API.Models.DTOs.Chat;
using ChatbotPlatform.API.Models.Entities;

namespace ChatbotPlatform.API.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<RegisterDto, User>();
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Company mappings
        CreateMap<Company, CompanyDto>();
        CreateMap<CreateCompanyDto, Company>();
        CreateMap<UpdateCompanyDto, Company>().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Address mapping (for nested objects)
        CreateMap<Address, Address>();

        // ContactDetails mapping (for nested objects)
        CreateMap<ContactDetails, ContactDetails>();

        // FAQ mappings
        CreateMap<FAQ, FAQDto>();
        CreateMap<CreateFAQDto, FAQ>();
        CreateMap<UpdateFAQDto, FAQ>();

        // Chat mappings
        CreateMap<ChatSession, ChatSessionDto>();
        CreateMap<ChatMessage, ChatMessageDto>();
    }
}