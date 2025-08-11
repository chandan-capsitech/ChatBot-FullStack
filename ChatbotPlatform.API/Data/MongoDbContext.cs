using ChatbotPlatform.API.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ChatbotPlatform.API.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
    {
        _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
    }

    // Collections
    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Company> Companies => _database.GetCollection<Company>("Companies");
    public IMongoCollection<FAQ> FAQs => _database.GetCollection<FAQ>("FAQs");
    public IMongoCollection<ChatSession> ChatSessions => _database.GetCollection<ChatSession>("ChatSessions");

    // Generic collection access
    // Dynamic collection access
    public IMongoCollection<T> GetCollection<T>() where T : BaseEntity
    {
        var collectionName = typeof(T).Name.ToLowerInvariant() + "s";
        return _database.GetCollection<T>(collectionName);
    }
}