using AutoMapper;
using ChatbotPlatform.API.Data;
using ChatbotPlatform.API.Models.DTOs.Company;
using ChatbotPlatform.API.Models.DTOs.Auth;
using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Utilities;
using MongoDB.Driver;

namespace ChatbotPlatform.API.Services;

public class CompanyService
{
    private readonly MongoDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserService _userService;

    public CompanyService(MongoDbContext context, IMapper mapper, UserService userService)
    {
        _context = context;
        _mapper = mapper;
        _userService = userService;
    }

    public async Task<List<CompanyDto>> GetAllAsync()
    {
        var companies = await _context.Companies.Find(_ => true).ToListAsync();
        return _mapper.Map<List<CompanyDto>>(companies);
    }

    public async Task<CompanyDto> GetByIdAsync(string id)
    {
        var company = await _context.Companies.Find(c => c.Id == id).FirstOrDefaultAsync();

        if (company == null)
        {
            throw new Exception("Company not found");
        }

        return _mapper.Map<CompanyDto>(company);
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyDto createCompanyDto)
    {

        var existComp = await _context.Companies.Find(c => c.CompanyName == createCompanyDto.CompanyName).FirstOrDefaultAsync();
        if (existComp != null)
        {
            throw new InvalidOperationException("Company already exist");
            //return;
        }

        if (createCompanyDto.Domains?.Any() == true)
        {
            foreach (var domain in createCompanyDto.Domains)
            {
                var existingCompany = await _context.Companies.Find(c => c.Domains.Contains(domain)).FirstOrDefaultAsync();

                if (existingCompany != null)
                {
                    throw new InvalidOperationException($"Domain {domain} already exists");
                }
            }
        }

        var company = _mapper.Map<Company>(createCompanyDto);

        // Set subscription limits based on subscription type
        company.SubscriptionLimits = SubscriptionDefaults.GetLimitsForSubscription(createCompanyDto.Subscription);
        company.CreatedAt = DateTime.UtcNow;
        company.UpdatedAt = DateTime.UtcNow;
        company.Status = CompanyStatus.Active;
        company.AdminCount = 0;  // will be increment when admin created
        company.EmployeeCount = 0;

        await _context.Companies.InsertOneAsync(company);

        return _mapper.Map<CompanyDto>(company);
    }

    public async Task<CompanyCreationResponseDto> CreateWithAdminAsync(CreateCompanyWithAdminDto dto, string createdBy)
    {
        // create company 1st
        var company = await CreateAsync(dto.CompanyDetails);

        // create admin user
        var adminDto = new CreateUserDto
        {
            Email = dto.AdminDetails.Email,
            Password = dto.AdminDetails.Password,
            FirstName = dto.AdminDetails.FirstName,
            LastName = dto.AdminDetails.LastName,
            Role = UserRole.Admin,
            CompanyId = company.Id,
            PhoneNumber = dto.AdminDetails.PhoneNumber,
            Timezone = dto.AdminDetails.Timezone,
            CreatedBy = createdBy,
        };


        var admin = await _userService.CreateAsync(adminDto);

        return new CompanyCreationResponseDto
        {
            Company = company,
            Admin = admin,
        };
    }

    public async Task<CompanyDto> UpdateAsync(string id, UpdateCompanyDto updateCompanyDto)
    {
        var existingCompany = await _context.Companies.Find(c => c.Id == id).FirstOrDefaultAsync();

        if (existingCompany == null)
        {
            throw new Exception("Company not found");
        }

        // manual mapping for null checks
        if (!string.IsNullOrEmpty(updateCompanyDto.CompanyName))
        {
            existingCompany.CompanyName = updateCompanyDto.CompanyName;
        }

        if (!string.IsNullOrEmpty(updateCompanyDto.CompanyType))
        {
            existingCompany.CompanyType = updateCompanyDto.CompanyType;
        }

        if (updateCompanyDto.SubscriptionType.HasValue)
        {
            existingCompany.Subscription = updateCompanyDto.SubscriptionType.Value;
        }

        if (updateCompanyDto.Domains != null)
        {
            existingCompany.Domains = updateCompanyDto.Domains;
        }

        if (updateCompanyDto.Address != null)
        {
            if (existingCompany.Address == null)
            {
                existingCompany.Address = new Address();
            }

            existingCompany.Address.AddressName = updateCompanyDto.Address.AddressName ?? existingCompany.Address.AddressName;
            existingCompany.Address.AddressType = updateCompanyDto.Address.AddressType;
            existingCompany.Address.City = updateCompanyDto.Address.City ?? existingCompany.Address.City;
            existingCompany.Address.Street = updateCompanyDto.Address.Street ?? existingCompany.Address.Street;
            existingCompany.Address.PinCode = updateCompanyDto.Address.PinCode ?? existingCompany.Address.PinCode;
            existingCompany.Address.District = updateCompanyDto.Address.District ?? existingCompany.Address.District;
            existingCompany.Address.State = updateCompanyDto.Address.State ?? existingCompany.Address.State;
            existingCompany.Address.Country = updateCompanyDto.Address.Country ?? existingCompany.Address.Country;
        }

        if (updateCompanyDto.ContactDetails != null)
        {
            if (existingCompany.ContactDetails == null)
            {
                existingCompany.ContactDetails = new ContactDetails();
            }

            existingCompany.ContactDetails.Name = updateCompanyDto.ContactDetails.Name ?? existingCompany.ContactDetails.Name;
            existingCompany.ContactDetails.Designation = updateCompanyDto.ContactDetails.Designation ?? existingCompany.ContactDetails.Designation;
            existingCompany.ContactDetails.PrimaryEmail = updateCompanyDto.ContactDetails.PrimaryEmail ?? existingCompany.ContactDetails.PrimaryEmail;
            existingCompany.ContactDetails.SupportPhone = updateCompanyDto.ContactDetails.SupportPhone ?? existingCompany.ContactDetails.SupportPhone;
            existingCompany.ContactDetails.CC = updateCompanyDto.ContactDetails.CC ?? existingCompany.ContactDetails.CC;
        }

        if (updateCompanyDto.EmployeeCount.HasValue)
        {
            existingCompany.EmployeeCount = updateCompanyDto.EmployeeCount.Value;
        }

        if (updateCompanyDto.Status.HasValue)
        {
            existingCompany.Status = updateCompanyDto.Status.Value;
        }
        //_mapper.Map(updateCompanyDto, existingCompany);
        existingCompany.UpdatedAt = DateTime.UtcNow;

        await _context.Companies.ReplaceOneAsync(c => c.Id == id, existingCompany);

        return _mapper.Map<CompanyDto>(existingCompany);
    }

    public async Task DeleteAsync(string id)
    {
        var result = await _context.Companies.DeleteOneAsync(c => c.Id == id);
        if (result.DeletedCount == 0)
        {
            throw new Exception("Company not found");
        }
    }

    public async Task<List<CompanyDto>> GetByStatusAsync(CompanyStatus status)
    {
        var companies = await _context.Companies.Find(c => c.Status == status).ToListAsync();
        return _mapper.Map<List<CompanyDto>>(companies);
    }

    public async Task<string> GetCompanyStatusAsync(string companyId)
    {
        var company = await _context.Companies.Find(c => c.Id == companyId).FirstOrDefaultAsync();
        return company.Status == 0 ? "Active" : "other";
    }
}