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


    // Read only company staffs(admin & emp)
    // super admin can not access specific company faqs

    [HttpGet("company/{companyId}")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<List<FAQDto>>> GetByCompany(string companyId)
    {
        var res = new ApiResponse<List<FAQDto>>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // superadmin has no access
            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin cannot access company-specific FAQs";
                return res;
            }

            // other company users can not
            if (currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied to this company";
                return res;
            }

            var faqs = await _faqService.GetByCompanyAsync(companyId);
            res.Status = true;
            res.Message = "FAQs retrieved successfully";
            res.Result = faqs;
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
            // Check if user has access to this FAQ's company
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin can not access company-specific FAQs";
                return res;
            }

            var faq = await _faqService.GetByIdAsync(id);
            // only own company users allowed
            if (currentUserCompanyId != faq.CompanyId)
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

    [HttpGet("company/{companyId}/search")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<List<FAQDto>>> Search(string companyId, [FromQuery] string searchTerm)
    {
        var res = new ApiResponse<List<FAQDto>>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;


            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin can not access company-specific FAQs";
                return res;
            }

            if (currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied for other company users";
                return res;
            }

            var faqs = await _faqService.SearchAsync(companyId, searchTerm);
            res.Status = true;
            res.Message = $"Found {faqs.Count()} FAQs matching '{searchTerm}'";
            res.Result = faqs;
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

            // superadmin has no access
            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin cannot access company-specific FAQs";
                return res;
            }

            // other company users can not access
            if (currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied to this company's FAQs";
                return res;
            }

            var faqs = await _faqService.GetTopLevelAsync(companyId);
            res.Status = true;
            res.Message = "Top-level FAQs retrieved successfully";
            res.Result = faqs;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<ApiResponse<FAQStatsDto>> GetFAQStats(string companyId)
    {
        var res = new ApiResponse<FAQStatsDto>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin cannot access company-specific FAQs";
                return res;
            }

            if (currentUserCompanyId != companyId)
            {
                res.Status = false;
                res.Message = "Access denied to this company's FAQs";
                return res;
            }

            var stats = await _faqService.GetFAQStatsAsync(companyId);
            res.Status = true;
            res.Message = "FAQ statistics retrieved successfully";
            res.Result = stats;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }


    // post - only company admins
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


            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin can not create company specific FAQs";
                return res;
            }

            if (currentUserRole != "Admin")
            {
                res.Status = false;
                res.Message = "Only company admin can create the FAQs";
                return res;
            }

            var faq = await _faqService.CreateAsync(dto, currentUserId, currentUserCompanyId);
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
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin cannot update company-specific FAQs. Only Company Admins can manage FAQs.";
                return res;
            }

            if (currentUserRole != "Admin")
            {
                res.Status = false;
                res.Message = "Only Company Admins can update FAQs";
                return res;
            }

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
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var currentUserCompanyId = User.FindFirst("companyId")?.Value!;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole == "SuperAdmin")
            {
                res.Status = false;
                res.Message = "SuperAdmin cannot Delete company-specific FAQs. Only Company Admins can manage FAQs.";
                return res;
            }

            if (currentUserRole != "Admin")
            {
                res.Status = false;
                res.Message = "Only Company Admins can Delete FAQs";
                return res;
            }

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
}