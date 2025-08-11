namespace ChatbotPlatform.API.Models.DTOs.FAQ
{
    public class UpdateFAQDto
    {
        public int Depth { get; set; } = 1;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public List<CreateFAQDto>? Options { get; set; } = new();
    }
}