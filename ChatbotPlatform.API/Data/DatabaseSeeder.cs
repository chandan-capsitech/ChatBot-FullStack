using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Utilities;
using MongoDB.Driver;

namespace ChatbotPlatform.API.Data;

public class DatabaseSeeder
{
    private readonly MongoDbContext _context;

    // log messages that are associated with the DatabaseSeeder class.
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(MongoDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedSuperAdminAsync();
            await SeedSampleCompanyAsync();
            await SeedSampleFAQsAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error occured during database seeding");
        }
    }

    // Default super-admin creation
    private async Task SeedSuperAdminAsync()
    {
        var existingSuperAdmin = await _context.Users.Find(u => u.Email == "superadmin@capsitech.com").FirstOrDefaultAsync();

        // if superadmin exist
        if (existingSuperAdmin != null)
        {
            _logger.LogInformation("SuperAdmin already exists");
            return;
        }

        // Default superadmin
        var superAdmin = new User
        {
            Email = "superadmin@capsitech.com",
            PasswordHash = PasswordHelper.HashPassword("SuperAdmin123"),
            Role = UserRole.SuperAdmin,
            CompanyId = string.Empty,
            Name = new UserName
            {
                FirstName = "Super",
                LastName = "Admin",
                DisplayName = "Super Admin"
            },
            Status = UserStatus.Active,
            Timezone = "UTC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // if no superadmin exists
        await _context.Users.InsertOneAsync(superAdmin);
        _logger.LogInformation("Superadmin created successfully");
    }


    // Default company, admin, Employee creation
    private async Task SeedSampleCompanyAsync()
    {
        //var existingSuperAdmin = await _context.Users.Find(u => u.Email == "superadmin@capsitech.com").FirstOrDefaultAsync();

        var existingCompany = await _context.Companies.Find(c => c.CompanyName == "Sample Company").FirstOrDefaultAsync();

        if (existingCompany != null)
        {
            _logger.LogInformation("Company already exists, skipping creation");
            return;
        }

        // Default company
        var company = new Company
        {
            CompanyName = "Sample Company",
            CompanyType = "Technology",
            //CreatedBy = existingSuperAdmin.Id,
            Subscription = SubscriptionType.Pro,
            SubscriptionLimits = SubscriptionDefaults.GetLimitsForSubscription(SubscriptionType.Pro),
            Domains = new List<string> { "sample.com", "demo.sample.com" },
            Status = CompanyStatus.Active,
            EmployeeCount = 0,
            AdminCount = 0,
            Address = new Address
            {
                AddressName = "Main Office",
                AddressType = AddressType.Office,
                City = "New York",
                Street = "123 Tech Street",
                PinCode = "10001",
                District = "Manhattan",
                State = "NY",
                Country = "USA"
            },
            ContactDetails = new ContactDetails
            {
                Name = "John Doe",
                Designation = "CTO",
                PrimaryEmail = "contact@sample.com",
                SupportPhone = "+1-555-0123"
            },
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        await _context.Companies.InsertOneAsync(company);

        // Default admin
        var admin = new User
        {
            Email = "admin@sample.com",
            PasswordHash = PasswordHelper.HashPassword("Admin123"),
            Role = UserRole.Admin,
            CompanyId = company.Id,
            Name = new UserName
            {
                FirstName = "John",
                LastName = "Admin",
                DisplayName = "John Admin"
            },
            Status = UserStatus.Active,
            Timezone = "UTC",
            CreatedBy = company.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _context.Users.InsertOneAsync(admin);

        // Default employee
        var employee = new User
        {
            Email = "employee@sample.com",
            PasswordHash = PasswordHelper.HashPassword("Employee123"),
            Role = UserRole.Employee,
            CompanyId = company.Id,
            Name = new UserName
            {
                FirstName = "Jane",
                LastName = "Employee",
                DisplayName = "Jane Employee"
            },
            Status = UserStatus.Active,
            Timezone = "UTC",
            CreatedBy = admin.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _context.Users.InsertOneAsync(employee);

        // update company counts
        await _context.Companies.UpdateOneAsync(
            c => c.Id == company.Id,
            Builders<Company>.Update
                .Set(c => c.AdminCount, 1)
                .Set(c => c.EmployeeCount, 1)
        );

        _logger.LogInformation("Cmpany and it's user created successfully");
    }


    // Default FAQ creation
    private async Task SeedSampleFAQsAsync()
    {
        var company = await _context.Companies.Find(c => c.CompanyName == "Sample Company").FirstOrDefaultAsync();

        if (company == null) return;

        var existingFAQs = await _context.FAQs.Find(f => f.CompanyId == company.Id).FirstOrDefaultAsync();

        if (existingFAQs != null)
        {
            _logger.LogInformation("Sample FAQs already exist, skipping creation");
            return;
        }

        var admin = await _context.Users
            .Find(u => u.CompanyId == company.Id && u.Role == UserRole.Admin)
            .FirstOrDefaultAsync();

        if (admin == null) return;

        var faqs = new List<FAQ>
        {
            new FAQ
            {
                CompanyId = company.Id,
                Question = "What are your business hours?",
                Answer = "We are open Monday to Friday, 9 AM to 6 PM EST. Our customer support is available 24/7 through this chat.",
                Depth = 1,
                Options = new List<FAQ>
                {
                    new FAQ
                    {
                        CompanyId = company.Id,
                        Question = "What about weekends?",
                        Answer = "We offer limited support on weekends for urgent issues. Please describe your issue and we'll get back to you as soon as possible.",
                        Depth = 2,
                        Options = new List<FAQ>
                        {
                            new FAQ
                            {
                                CompanyId = company.Id,
                                Question = "What courses do we offer",
                                Answer = "We offer BCA, MCA, integrated course",
                                Depth = 3,
                                CreatedBy = admin.Id,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            },
                        },
                        CreatedBy = admin.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new FAQ
                    {
                        CompanyId = company.Id,
                        Question = "How do I Apply?",
                        Answer = "You can apply through our website",
                        Depth = 2,
                        Options = new List<FAQ>
                        {
                            new FAQ
                            {
                                CompanyId = company.Id,
                                Question = "view curriculum",
                                Answer = "Check out our website xyz.com",
                                Depth= 3,
                                CreatedBy = admin.Id,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            },
                        },
                        CreatedBy = admin.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                },
                CreatedBy = company.Id,
                UpdatedBy = company.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new FAQ
            {
                CompanyId = company.Id,
                Question = "How do I reset my password?",
                Answer = "To reset your password, go to the login page and click 'Forgot Password'. Enter your email address and we'll send you reset instructions.",
                Depth = 1,
                Options = new List<FAQ>(),
                CreatedBy = company.Id,
                UpdatedBy = company.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.FAQs.InsertManyAsync(faqs);
        _logger.LogInformation("Sample FAQ created successfully");
    }
}