using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebSiteData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly ICategoryService _categoryService;

    public CategoriesController(ILogger<CategoriesController> logger, ICategoryService categoryService)
    {
        _logger = logger;
        _categoryService = categoryService;
    }

    [HttpPost("create")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ResponseModel<CategoryResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto request)
    {
        var result = new ResponseModel<CategoryResponseDto>();
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var currentUser = User.Identity?.Name ?? "Admin"; // Lấy tên người đang login
            var categoryResult = await _categoryService.CreateCategoryAsync(request, currentUser);

            if (categoryResult.IsFailure)
            {
                _logger.LogError("Failed to create category: {Error}", categoryResult.Error);
                result.Status = 400;
                result.Message = categoryResult.Error;
                return BadRequest(result);
            }

            result.Status = 201;
            result.Data = categoryResult.Value;
            result.Message = "Category created successfully";
            return CreatedAtAction(nameof(GetCategoryById), new { id = categoryResult.Value.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating category");
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Lấy chi tiết một danh mục theo ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseModel<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var result = new ResponseModel<CategoryResponseDto>();
        try
        {
            var categoryResult = await _categoryService.GetCategoryByIdAsync(id);

            if (categoryResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve category: {Error}", categoryResult.Error);
                result.Status = 404;
                result.Message = categoryResult.Error;
                return NotFound(result);
            }

            result.Status = 200;
            result.Data = categoryResult.Value;
            result.Message = "Category retrieved successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving category with ID: {CategoryId}", id);
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết của một danh mục
    /// </summary>
    /// <param name="id">ID của danh mục cần cập nhật</param>
    /// <param name="request">Dữ liệu cập nhật (Name, Description)</param>
    [HttpPut("update/{id}")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ResponseModel<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory([FromRoute] Guid id, [FromBody] UpdateCategoryRequestDto request)
    {
        var result = new ResponseModel<CategoryResponseDto>();
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid category update request");
                return BadRequest(ModelState);
            }

            var currentUser = User.Identity?.Name ?? "Admin";

            var categoryResult = await _categoryService.UpdateCategoryAsync(id, request, currentUser);

            if (categoryResult.IsFailure)
            {
                _logger.LogError("Failed to update category: {Error}", categoryResult.Error);

                if (categoryResult.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    result.Status = 404;
                    result.Message = categoryResult.Error;
                    return NotFound(result);
                }

                result.Status = 400;
                result.Message = categoryResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = categoryResult.Value;
            result.Message = "Category updated successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating category with ID: {CategoryId}", id);
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Cập nhật trạng thái hiển thị (IsActive) của danh mục
    /// </summary>
    [HttpPatch("update-status")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatusCategory([FromBody] UpdateStatusCategoryRequestDto requestDto)
    {
        var result = new ResponseModel<string>();
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var currentUser = User.Identity?.Name ?? "Admin";
            var categoryResult = await _categoryService.UpdateStatusAsync(requestDto, currentUser);

            if (categoryResult.IsFailure)
            {
                result.Status = 400;
                result.Message = categoryResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = categoryResult.Value;
            result.Message = "Update status success";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating category status.");
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Xóa một danh mục khỏi hệ thống
    /// </summary>
    [HttpDelete("delete/{id}")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = new ResponseModel<string>();
        try
        {
            var categoryResult = await _categoryService.DeleteAsync(id);
            if (categoryResult.IsFailure)
            {
                result.Status = 400;
                result.Message = categoryResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Message = categoryResult.Value;
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting category.");
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả danh mục (dùng cho Dropdown lúc tạo Product)
    /// </summary>
    [HttpGet("get-all")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseModel<List<CategoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = new ResponseModel<List<CategoryResponseDto>>();
        try
        {
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();

            if (categoriesResult.IsFailure)
            {
                result.Status = 400;
                result.Message = categoriesResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = categoriesResult.Value;
            result.Message = "Categories retrieved successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all categories");
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Lấy danh sách danh mục có phân trang (dùng cho bảng quản lý ở Admin)
    /// </summary>
    [HttpGet("get-all-paged")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseModel<PagedResult<CategoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllCategoriesPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = new ResponseModel<PagedResult<CategoryResponseDto>>();
        try
        {
            var categoriesResult = await _categoryService.GetAllCategoriesPagedAsync(pageNumber, pageSize);

            if (categoriesResult.IsFailure)
            {
                result.Status = 400;
                result.Message = categoriesResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = categoriesResult.Value;
            result.Message = "Paged categories retrieved successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving paged categories");
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }
}