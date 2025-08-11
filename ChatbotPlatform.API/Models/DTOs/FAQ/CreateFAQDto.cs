
using System.ComponentModel.DataAnnotations;

namespace ChatbotPlatform.API.Models.DTOs.FAQ
{
    public class CreateFAQDto
    {
        public string CompanyId { get; set; } = string.Empty; // Will be auto-set for admins
        public int Depth { get; set; } = 1;

        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        public List<CreateFAQDto>? Options { get; set; } = new();
    }
}