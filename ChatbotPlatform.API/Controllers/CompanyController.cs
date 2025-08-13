using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatbotPlatform.API.Models;
using ChatbotPlatform.API.Models.DTOs.Company;
using ChatbotPlatform.API.Models.Entities;
using ChatbotPlatform.API.Services;
using System.Security.Claims;

namespace ChatbotPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly CompanyService _companyService;

    public CompanyController(CompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<List<CompanyDto>>> GetAll()
    {
        var res = new ApiResponse<List<CompanyDto>>();
        try
        {
            var companies = await _companyService.GetAllAsync();
            res.Status = true;
            res.Message = "Companies retrieved successfully";
            res.Result = companies;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<CompanyDto>> GetById(string id)
    {
        var res = new ApiResponse<CompanyDto>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Check if user has access to this company
            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != id)
            {
                res.Status = false;
                res.Message = "Access denied to this company";
                return res;
            }

            var company = await _companyService.GetByIdAsync(id);
            res.Status = true;
            res.Message = "Company retrieved successfully";
            res.Result = company;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    // Create company with admin in single call
    [HttpPost("with-admin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<CompanyCreationResponseDto>> CreateWithAdmin(CreateCompanyWithAdminDto dto)
    {
        var res = new ApiResponse<CompanyCreationResponseDto>();
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

            var result = await _companyService.CreateWithAdminAsync(dto, currentUserId);

            res.Status = true;
            res.Message = "Company and admin created successfully";
            res.Result = result;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    // Create company only (for backwards compatibility)
    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<CompanyDto>> Create(CreateCompanyDto dto)
    {
        var res = new ApiResponse<CompanyDto>();
        try
        {
            var company = await _companyService.CreateAsync(dto);
            res.Status = true;
            res.Message = "Company created successfully";
            res.Result = company;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ApiResponse<CompanyDto>> PatchUpdate(string id, UpdateCompanyDto dto)
    {
        var res = new ApiResponse<CompanyDto>();
        try
        {
            var currentUserCompanyId = User.FindFirst("companyId")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Employee can not update anything
            if (currentUserRole == "Employee")
            {
                res.Status = false;
                res.Message = "Employee can not update anything";
                return res;
            }

            // Check if user has access to update this company
            if (currentUserRole != "SuperAdmin" && currentUserCompanyId != id)
            {
                res.Status = false;
                res.Message = "Access denied to update this company";
                return res;
            }

            // Define allowed fields based on role
            bool allowFullUpdate = currentUserRole == "SuperAdmin";

            var company = await _companyService.UpdateAsync(id, dto, allowFullUpdate);
            res.Status = true;
            res.Message = "Company updated successfully";
            res.Result = company;
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<object>> Delete(string id)
    {
        var res = new ApiResponse<object>();
        try
        {
            await _companyService.DeleteAsync(id);
            res.Status = true;
            res.Message = "Company deleted successfully";
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }

    [HttpGet("status/{status}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<List<CompanyDto>>> GetByStatus(CompanyStatus status)
    {
        var res = new ApiResponse<List<CompanyDto>>();
        try
        {
            var companies = await _companyService.GetByStatusAsync(status);
            res.Status = true;
            res.Message = $"Companies with status {status} retrieved successfully";
            res.Result = companies.ToList();
        }
        catch (Exception ex)
        {
            res.Status = false;
            res.Message = ex.Message;
        }
        return res;
    }
}