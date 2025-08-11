using MongoDB.Bson.Serialization.Attributes;
namespace ChatbotPlatform.API.Models.Entities
{
    public class FAQ : BaseEntity
    {
        [BsonElement("companyId")]
        public string CompanyId { get; set; } = string.Empty;

        [BsonElement("depth")]
        public int Depth { get; set; } = 1;

        [BsonElement("question")]
        public string Question { get; set; } = string.Empty;

        [BsonElement("answer")]
        public string Answer { get; set; } = string.Empty;

        [BsonElement("options")]
        public List<FAQ>? Options { get; set; } = new();

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [BsonElement("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}