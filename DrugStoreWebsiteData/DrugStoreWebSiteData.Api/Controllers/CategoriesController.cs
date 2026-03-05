using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DrugStoreWebSiteData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryService categoryService,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">The category creation request data.</param>
    /// <returns>Returns a CategoryDto object if successful.</returns>
    /// <response code="201">Category created successfully.</response>
    /// <response code="400">Invalid data or existing name.</response>
    [HttpPost("create")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto request)
    {
        var result = new ResponseModel<CategoryResponseDto>();
        try
        {
            if(!ModelState.IsValid)
            {
                result.Message = "Invalid data.";
                return BadRequest(result);
            }
            var categoryResult = await _categoryService.CreateCategoryAsync(request);
            if (categoryResult.IsFailure)
            {
                _logger.LogError("Failed to create category: {Error}", categoryResult.Error);
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
    /// Get a specific category by its unique id.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the category.</param>
    /// <returns>Returns a CategoryDto object.</returns>
    /// <response code="200">Category found.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
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
            result.Data =  categoryResult.Value;
            result.Message = "Category retrieved successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving category");
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Get all categories.
    /// </summary>
    /// <returns>Returns a list of CategoryDto objects.</returns>
    /// <response code="200">Categories found.</response>
    /// <response code="400">Error retrieving categories.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = new ResponseModel<List<CategoryResponseDto>>();
        try
        {
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve categories: {Error}", categoriesResult.Error);
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
            _logger.LogError(ex, "Error occurred while retrieving categories");
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }
}