using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatbotPlatform.API.Models;
using ChatbotPlatform.API.Models.DTOs.FAQ;
using ChatbotPlatform.API.Services;
using System.Security.Claims;

namespace ChatbotPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FAQController : ControllerBase
{
    private readonly FAQService _faqService;

    public FAQController(FAQService faqService)
    {
        _faqService = faqService;
    }

    [HttpGet("company/{companyId}")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<List<FAQDto>>> GetByCompany(string companyId)
    {
        var res = new ApiResponse<List<FAQDto>>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Users can only see FAQs for their own company
            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied to this company's FAQs";
                return res;
            }

            var faqs = await _faqService.GetByCompanyAsync(companyId);
            res.Status = true;
            res.Message = "FAQs retrieved successfully";
            res.Result = faqs.ToList();
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<FAQDto>> GetById(string id)
    {
        var res = new ApiResponse<FAQDto>();
        try
        {
            var faq = await _faqService.GetByIdAsync(id);

            // Check if user has access to this FAQ's company
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != faq.CompanyId)
            {
                res.Status = false;
                res.Message = "Access denied to this FAQ";
                return res;
            }

            res.Status = true;
            res.Message = "FAQ retrieved successfully";
            res.Result = faq;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<FAQDto>> Create(CreateFAQDto dto)
    {
        var res = new ApiResponse<FAQDto>();
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var currentUserCompanyId = User.FindFirst("companyId")?.Value!;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // For Company Admins, force their own company ID
            var targetCompanyId = currentUserRole == "SuperAdmin" ? dto.CompanyId : currentUserCompanyId;

            var faq = await _faqService.CreateAsync(dto, currentUserId, targetCompanyId);
            res.Status = true;
            res.Message = "FAQ created successfully";
            res.Result = faq;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<FAQDto>> Update(string id, UpdateFAQDto dto)
    {
        var res = new ApiResponse<FAQDto>();
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var currentUserCompanyId = User.FindFirst("companyId")?.Value!;

            var faq = await _faqService.UpdateAsync(id, dto, currentUserId, currentUserCompanyId);
            res.Status = true;
            res.Message = "FAQ updated successfully";
            res.Result = faq;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<object>> Delete(string id)
    {
        var res = new ApiResponse<object>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value!;

            await _faqService.DeleteAsync(id, currentUserCompanyId);
            res.Status = true;
            res.Message = "FAQ deleted successfully";
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("company/{companyId}/search")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<List<FAQDto>>> Search(string companyId, [FromQuery] string searchTerm)
    {
        var res = new ApiResponse<List<FAQDto>>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Users can only search FAQs for their own company
            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied to search this company's FAQs";
                return res;
            }

            var faqs = await _faqService.SearchAsync(companyId, searchTerm);
            res.Status = true;
            res.Message = $"Found {faqs.Count()} FAQs matching '{searchTerm}'";
            res.Result = faqs.ToList();
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("company/{companyId}/top-level")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<List<FAQDto>>> GetTopLevel(string companyId)
    {
        var res = new ApiResponse<List<FAQDto>>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Users can only see FAQs for their own company
            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied to this company's FAQs";
                return res;
            }

            var faqs = await _faqService.GetTopLevelAsync(companyId);
            res.Status = true;
            res.Message = "Top-level FAQs retrieved successfully";
            res.Result = faqs.ToList();
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }
}