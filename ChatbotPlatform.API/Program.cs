using ChatbotPlatform.API.Data;
using ChatbotPlatform.API.Hubs;
using ChatbotPlatform.API.Services;
using ChatbotPlatform.API.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// MongoDB Configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    if (settings == null)
    {
        throw new InvalidOperationException("MongoDbSettings configuration is missing");
    }

    var mongoClientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
    mongoClientSettings.MaxConnectionPoolSize = settings.MaxConnectionPoolSize;
    mongoClientSettings.ServerSelectionTimeout = settings.ServerSelectionTimeout;
    mongoClientSettings.ConnectTimeout = settings.ConnectTimeout;

    return new MongoClient(mongoClientSettings);
});

// Register MongoDB Context
builder.Services.AddScoped<MongoDbContext>();

// Service Registration (No interfaces)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<FAQService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<JwtService>();

// Register Database Seeder
builder.Services.AddScoped<DatabaseSeeder>();

// AutoMapper Configuration
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<ChatbotPlatform.API.Models.Entities.User, ChatbotPlatform.API.Models.DTOs.Auth.UserDto>();
    cfg.CreateMap<ChatbotPlatform.API.Models.DTOs.Auth.RegisterDto, ChatbotPlatform.API.Models.Entities.User>();
    cfg.CreateMap<ChatbotPlatform.API.Models.Entities.Company, ChatbotPlatform.API.Models.DTOs.Company.CompanyDto>();
    cfg.CreateMap<ChatbotPlatform.API.Models.DTOs.Company.CreateCompanyDto, ChatbotPlatform.API.Models.Entities.Company>();
    cfg.CreateMap<ChatbotPlatform.API.Models.Entities.FAQ, ChatbotPlatform.API.Models.DTOs.FAQ.FAQDto>();
    cfg.CreateMap<ChatbotPlatform.API.Models.DTOs.FAQ.CreateFAQDto, ChatbotPlatform.API.Models.Entities.FAQ>();
    cfg.CreateMap<ChatbotPlatform.API.Models.Entities.ChatSession, ChatbotPlatform.API.Models.DTOs.Chat.ChatSessionDto>();
    cfg.CreateMap<ChatbotPlatform.API.Models.Entities.ChatMessage, ChatbotPlatform.API.Models.DTOs.Chat.ChatMessageDto>();
});
// Initialize JWT Helper
JwtHelper.Initialize(builder.Configuration);

// JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured");
}

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("EmployeeOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin", "Employee"));
});


// CORS
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    corsOrigins = new[] { "http://localhost:3000", "https://localhost:3000" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Controllers with JSON enum support
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Chatbot Platform API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowedOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

// Database seeding
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application started successfully");
    logger.LogInformation("Swagger UI available at: /swagger");
}

app.Run();