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

    public async Task<CompanyDto> UpdateAsync(string id, UpdateCompanyDto updateCompanyDto, bool allowFullUpdate)
    {
        var existingCompany = await _context.Companies.Find(c => c.Id == id).FirstOrDefaultAsync();

        if (existingCompany == null)
        {
            throw new Exception("Company not found");
        }

        // manual mapping for null checks

        if (allowFullUpdate)
        {
            ApplyFullUpdate(existingCompany, updateCompanyDto);
        }
        else
        {
            ApplyAddressAndContactUpdate(existingCompany, updateCompanyDto);
        }

        //_mapper.Map(updateCompanyDto, existingCompany);
        existingCompany.UpdatedAt = DateTime.UtcNow;

        await _context.Companies.ReplaceOneAsync(c => c.Id == id, existingCompany);

        return _mapper.Map<CompanyDto>(existingCompany);
    }

    private void ApplyFullUpdate(Company existingCompany, UpdateCompanyDto dto)
    {
        if (!string.IsNullOrEmpty(dto.CompanyName))
        {
            existingCompany.CompanyName = dto.CompanyName;
        }

        if (!string.IsNullOrEmpty(dto.CompanyType))
        {
            existingCompany.CompanyType = dto.CompanyType;
        }

        if (dto.Subscription.HasValue)
        {
            existingCompany.Subscription = dto.Subscription.Value;
            existingCompany.SubscriptionLimits = SubscriptionDefaults.GetLimitsForSubscription(dto.Subscription.Value);
        }

        if (dto.Domains != null)
        {
            existingCompany.Domains = dto.Domains;
        }
        if (dto.EmployeeCount.HasValue)
        {
            existingCompany.EmployeeCount = dto.EmployeeCount.Value;
        }

        if (dto.Status.HasValue)
        {
            existingCompany.Status = dto.Status.Value;
        }

        ApplyAddressAndContactUpdate(existingCompany, dto);
    }

    private void ApplyAddressAndContactUpdate(Company existingCompany, UpdateCompanyDto dto)
    {
        if (dto.Address != null)
        {
            if (existingCompany.Address == null)
            {
                existingCompany.Address = new Address();
            }

            existingCompany.Address.AddressName = dto.Address.AddressName ?? existingCompany.Address.AddressName;
            existingCompany.Address.AddressType = dto.Address.AddressType;
            existingCompany.Address.City = dto.Address.City ?? existingCompany.Address.City;
            existingCompany.Address.Street = dto.Address.Street ?? existingCompany.Address.Street;
            existingCompany.Address.PinCode = dto.Address.PinCode ?? existingCompany.Address.PinCode;
            existingCompany.Address.District = dto.Address.District ?? existingCompany.Address.District;
            existingCompany.Address.State = dto.Address.State ?? existingCompany.Address.State;
            existingCompany.Address.Country = dto.Address.Country ?? existingCompany.Address.Country;
        }

        if (dto.ContactDetails != null)
        {
            if (existingCompany.ContactDetails == null)
            {
                existingCompany.ContactDetails = new ContactDetails();
            }

            existingCompany.ContactDetails.Name = dto.ContactDetails.Name ?? existingCompany.ContactDetails.Name;
            existingCompany.ContactDetails.Designation = dto.ContactDetails.Designation ?? existingCompany.ContactDetails.Designation;
            existingCompany.ContactDetails.PrimaryEmail = dto.ContactDetails.PrimaryEmail ?? existingCompany.ContactDetails.PrimaryEmail;
            existingCompany.ContactDetails.SupportPhone = dto.ContactDetails.SupportPhone ?? existingCompany.ContactDetails.SupportPhone;
            existingCompany.ContactDetails.CC = dto.ContactDetails.CC ?? existingCompany.ContactDetails.CC;
        }
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