using System.Security.Claims;
using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebSiteData.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;

    public BannersController(IBannerService bannerService)
    {
        _bannerService = bannerService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetActiveBanners()
    {
        var result = await _bannerService.GetActiveBannersAsync();
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllBanners()
    {
        var result = await _bannerService.GetAllBannersAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBanner([FromForm] CreateBannerRequestDto request)
    {
        var result = await _bannerService.CreateBannerAsync(request);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBanner(Guid id, [FromForm] UpdateBannerRequestDto request)
    {
        var result = await _bannerService.UpdateBannerAsync(id, request);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBanner(Guid id)
    {
        var result = await _bannerService.DeleteBannerAsync(id);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }
}