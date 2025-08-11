using AutoMapper;
using ChatbotPlatform.API.Data;
using ChatbotPlatform.API.Models.DTOs.Auth;
using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Utilities;
using MongoDB.Driver;

namespace ChatbotPlatform.API.Services;

public class UserService
{
    private readonly MongoDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(MongoDbContext context, IMapper mapper, ILogger<UserService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _context.Users.Find(_ => true).ToListAsync();
        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<List<UserDto>> GetByCompanyIdAsync(string companyId)
    {
        var users = await _context.Users.Find(u => u.CompanyId == companyId).ToListAsync();
        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<UserDto> GetByIdAsync(string id)
    {
        var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();

        if (user == null)
        {
            throw new Exception("User not found");
        }

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
    {
        // check if user already exists
        var existingUser = await _context.Users.Find(u => u.Email.ToLower() == createUserDto.Email.ToLower()).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Get company to check limits
        var company = await _context.Companies.Find(c => c.Id == createUserDto.CompanyId).FirstOrDefaultAsync();
        if (company == null)
        {
            throw new InvalidOperationException("Company not found");
        }

        // Check subscription limit
        await ValidateSubscriptionLimitsAsync(company, createUserDto.Role);

        var user = new User
        {
            Email = createUserDto.Email,
            PasswordHash = PasswordHelper.HashPassword(createUserDto.Password),
            CompanyId = createUserDto.CompanyId,
            Role = createUserDto.Role,
            Name = new UserName
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                DisplayName = $"{createUserDto.FirstName} {createUserDto.LastName}"
            },
            PhoneNumber = createUserDto.PhoneNumber,
            Department = createUserDto.Department,
            Status = UserStatus.Active,
            Timezone = createUserDto.Timezone ?? "UTC",
            CreatedBy = createUserDto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Insert user 1st
        await _context.Users.InsertOneAsync(user);

        // update company count
        await UpdateCompanyUserCountsAsync(createUserDto.CompanyId, createUserDto.Role, isIncrement: true);

        _logger.LogInformation("User created successfully: {Email} with role {Role} for company {CompanyId}", user.Email, createUserDto.Role, createUserDto.CompanyId);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserDto updateUserDto)
    {
        // check if user exist or not
        var existingUser = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (existingUser == null)
        {
            throw new Exception("User not found");
        }

        if (!string.IsNullOrEmpty(updateUserDto.FirstName))
        {
            existingUser.Name.FirstName = updateUserDto.FirstName;
        }

        if (!string.IsNullOrEmpty(updateUserDto.LastName))
        {
            existingUser.Name.LastName = updateUserDto.LastName;
        }

        if (!string.IsNullOrEmpty(updateUserDto.FirstName) || !string.IsNullOrEmpty(updateUserDto.LastName))
        {
            existingUser.Name.DisplayName = $"{existingUser.Name.FirstName} {existingUser.Name.LastName}";
        }

        if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
        {
            existingUser.PhoneNumber = updateUserDto.PhoneNumber;
        }

        if (!string.IsNullOrEmpty(updateUserDto.Department))
        {
            existingUser.Department = updateUserDto.Department;
        }

        if (!string.IsNullOrEmpty(updateUserDto.ProfilePic))
        {
            existingUser.ProfilePic = updateUserDto.ProfilePic;
        }

        if (!string.IsNullOrEmpty(updateUserDto.Timezone))
        {
            existingUser.Timezone = updateUserDto.Timezone;
        }

        if (updateUserDto.Role.HasValue)
        {
            existingUser.Role = updateUserDto.Role.Value;
        }

        if (updateUserDto.Status.HasValue)
        {
            existingUser.Status = updateUserDto.Status.Value;
        }

        existingUser.UpdatedAt = DateTime.UtcNow;

        await _context.Users.ReplaceOneAsync(u => u.Id == id, existingUser);
        _logger.LogInformation("User updated successfully: {Id}", id);

        return _mapper.Map<UserDto>(existingUser);
    }

    public async Task DeleteAsync(string id)
    {
        // check user exist or not
        var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            throw new Exception("User not found");
        }

        var result = await _context.Users.DeleteOneAsync(u => u.Id == id);
        if (result.DeletedCount == 0)
        {
            throw new Exception("Failed to delete user");
        }

        // Update company counts (decrement)
        await UpdateCompanyUserCountsAsync(user.CompanyId!, user.Role, isIncrement: false);

        _logger.LogInformation("User deleted successfully: {Id}", id);
    }

    public async Task<UserDto> UpdateStatusAsync(string id, UserStatus status)
    {
        // Check user exist or not
        var existingUser = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (existingUser == null)
        {
            throw new Exception("User not found");
        }

        existingUser.Status = status;
        existingUser.UpdatedAt = DateTime.UtcNow;

        await _context.Users.ReplaceOneAsync(u => u.Id == id, existingUser);
        _logger.LogInformation("User status updated: {Id} -> {Status}", id, status);

        return _mapper.Map<UserDto>(existingUser);
    }

    public async Task<List<UserDto>> GetByRoleAsync(UserRole role)
    {
        var users = await _context.Users.Find(u => u.Role == role).ToListAsync();
        return _mapper.Map<List<UserDto>>(users);
    }

    private async Task ValidateSubscriptionLimitsAsync(Company company, UserRole roleToCreate)
    {
        var limits = company.SubscriptionLimits;

        if (roleToCreate == UserRole.Admin)
        {
            if (company.AdminCount >= limits.MaxAdmins)
            {
                throw new InvalidOperationException($"Cannot create more admins. Your {company.Subscription} subscription allows maximum {limits.MaxAdmins} admins. Current: {company.AdminCount}");
            }
        }
        else if (roleToCreate == UserRole.Employee)
        {
            if (company.EmployeeCount >= limits.MaxEmployees)
            {
                throw new InvalidOperationException($"Cannot create more employees. Your {company.Subscription} subscription allows maximum {limits.MaxEmployees} employees. Current: {company.EmployeeCount}");
            }
        }
    }

    private async Task UpdateCompanyUserCountsAsync(string companyId, UserRole role, bool isIncrement)
    {
        var filter = Builders<Company>.Filter.Eq(c => c.Id, companyId);
        UpdateDefinition<Company> update;

        if (role == UserRole.Admin)
        {
            update = isIncrement ? Builders<Company>.Update.Inc(c => c.AdminCount, 1) : Builders<Company>.Update.Inc(c => c.AdminCount, -1);
        }
        else if (role == UserRole.Employee)
        {
            update = isIncrement ? Builders<Company>.Update.Inc(c => c.EmployeeCount, 1) : Builders<Company>.Update.Inc(c => c.EmployeeCount, -1);
        }
        else
        {
            return; // Don't update counts for SuperAdmin
        }

        // Always update the UpdatedAt timestamp
        update = update.Set(c => c.UpdatedAt, DateTime.UtcNow);

        await _context.Companies.UpdateOneAsync(filter, update);

        _logger.LogInformation("Updated company {CompanyId} {Role} count by {Change}", companyId, role, isIncrement ? "+1" : "-1");
    }
}