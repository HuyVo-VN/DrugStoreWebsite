using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Application.Services;
using DrugStoreWebSiteData.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;


namespace DrugStoreWebSiteData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    private readonly IProductService _productService;

    public ProductsController(ILogger<ProductsController> logger, IProductService productService)
    {
        _logger = logger;
        _productService = productService;
    }

    /// <summary>
    /// Creates a new product in the system.
    /// </summary>
    /// <param name="request">
    /// The product creation request data, including name, description, price, stock quantity, image URL, and category ID.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ProductDto"/> object if the product is successfully created.
    /// </returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Invalid product data or request format.</response>
    /// <response code="404">Specified category not found.</response>
    [HttpPost("create")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductRequestDto request)
    {
        var result = new ResponseModel<ProductResponseDto>();
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product creation request");
                return BadRequest(ModelState);
            }
            var productResult = await _productService.CreateProductAsync(request);

            if (productResult.IsFailure)
            {
                _logger.LogError("Failed to create product: {Error}", productResult.Error);
                if (productResult.Error.Contains("Category"))
                {
                    result.Status = 404;
                    result.Message = productResult.Error;
                    return NotFound(result);
                }
                result.Message = productResult.Error;
                return BadRequest(result);
            }

            result.Status = 201;
            result.Data = productResult.Value;
            result.Message = "Product created successfully";
            return CreatedAtAction(nameof(GetProductById), new { id = productResult.Value.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating product");
            result.Message = ex.Message;
            return BadRequest(result);
        }

    }

    /// <summary>
    /// Get a specific product by its unique id.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the product to retrieve.</param>
    /// <returns>
    /// Returns a <see cref="ProductDto"/> object containing product details if found.
    /// </returns>
    /// <response code="200">Product found and returned successfully.</response>
    /// <response code="404">No product found with the specified ID.</response>
    /// <response code="400">Invalid product ID or request format.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var result = new ResponseModel<ProductResponseDto>();
        try
        {
            var productResult = await _productService.GetProductByIdAsync(id);

            if (productResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve product: {Error}", productResult.Error);
                result.Status = 404;
                result.Message = productResult.Error;
                return NotFound(result);
            }

            result.Status = 200;
            result.Data = productResult.Value;
            result.Message = "Product retrieved successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving product with ID: {ProductId}", id);
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Update an existing product.
    /// </summary>
    [HttpPut("update/{id}")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromForm] UpdateProductRequestDto request)
    {
        var result = new ResponseModel<ProductResponseDto>();
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product update request");
                return BadRequest(ModelState);
            }

            var productResult = await _productService.UpdateProductAsync(id, request);

            if (productResult.IsFailure)
            {
                _logger.LogError("Failed to update product: {Error}", productResult.Error);
                if (productResult.Error.Contains("Category"))
                {
                    result.Status = 404;
                    result.Message = productResult.Error;
                    return NotFound(result);
                }
                result.Status = 400;
                result.Message = productResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = productResult.Value;
            result.Message = "Product updated successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating product with ID: {ProductId}", id);
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Get all products.
    /// </summary>
    [HttpGet("get-all")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseModel<PagedResult<ProductResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = new ResponseModel<PagedResult<ProductResponseDto>>();
        try
        {
            var productsResult = await _productService.GetAllProductsAsync(pageNumber, pageSize);


            if (productsResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve products: {Error}", productsResult.Error);
                result.Status = 400;
                result.Message = productsResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = productsResult.Value;
            result.Message = "Products retrieved successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all products");
            result.Status = 400;
            result.Message = ex.Message;
            return BadRequest(result);
        }
    }

    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpDelete("delete-product")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    public async Task<IActionResult> DeleteProduct([FromBody] DeleteProductRequestDto requestDto)
    {
        var result = new ResponseModel<string>();
        try
        {
            var productResult = await _productService.DeleteAsync(requestDto.ProductId);
            if (productResult.IsFailure)
                return BadRequest(productResult.Error);
            result.Status = 200;
            result.Message = productResult.Value;
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while deleting product: {ex.Message}"));
        }
    }


    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpPatch("update-status-product")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    public async Task<IActionResult> UpadateStatusProduct([FromBody] UpdateStatusProductRequestDto requestDto)
    {
        try
        {
            var productResult = await _productService.UpdateStatusAsync(requestDto);
            if (productResult.IsFailure)
                return BadRequest(productResult.Error);

            return Ok(productResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while updating status of product: {ex.Message}");
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while updating status product: {ex.Message}"));
        }
    }

    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet("search-product")]
    public async Task<IActionResult> SearchProducts([FromQuery] SearchProductRequestDto requestDto)
    {
        try
        {
            var searchResult = await _productService.SearchProductsAsync(requestDto);

            if (searchResult.IsFailure)
            {
                _logger.LogError($"Failed to search products: {searchResult.Error}");
                return BadRequest(searchResult.Error);
            }
            return Ok(searchResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while searching products: {ex.Message}");
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while searching product: {ex.Message}"));
        }
    }

    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet("filter-products")]
    public async Task<IActionResult> FilterProducts([FromQuery] FilterProductRequestDto requestDto)
    {
        try
        {
            var filterResult = await _productService.FilterProductsAsync(requestDto);

            if (filterResult.IsFailure)
            {
                _logger.LogError($"Failed to filter products: {filterResult.Error}");
                return BadRequest(filterResult.Error);
            }
            return Ok(filterResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while filtering products: {ex.Message}");
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while filtering product: {ex.Message}"));
        }
    }

    [HttpPatch("cancel-sale/{id}")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSale(Guid id)
    {
        var result = new ResponseModel<string>();
        try
        {
            var cancelResult = await _productService.CancelSaleAsync(id);

            if (cancelResult.IsFailure)
            {
                if (cancelResult.Error.Contains("Can not find"))
                {
                    result.Status = 404;
                    result.Message = cancelResult.Error;
                    return NotFound(result);
                }

                result.Status = 400;
                result.Message = cancelResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Message = cancelResult.Value;
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while disabling sales for product ID: {ProductId}", id);
            result.Status = 400;
            result.Message = $"An unknown error has occurred.: {ex.Message}";
            return BadRequest(result);
        }
    }

    [AllowAnonymous]
    [HttpGet("sale-products")]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaleProducts([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 15)
    {
        var result = await _productService.GetSaleProductsPagedAsync(pageIndex, pageSize);

        if (result.IsSuccess)
        {
            return Ok(new
            {
                status = 200,
                data = result.Value
            });
        }

        return BadRequest(new
        {
            status = 400,
            message = result.Error
        });
    }

    [AllowAnonymous]
    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellers([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 15)
    {
        var result = await _productService.GetBestSellersPagedAsync(pageIndex, pageSize);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result.Error);
    }

    [AllowAnonymous]
    [HttpGet("{id}/products")]
    public async Task<IActionResult> GetProductsByCollection(Guid id, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 15)
    {
        var result = await _productService.GetProductsByCollectionPagedAsync(id, pageIndex, pageSize);

        if (result.IsSuccess)
        {
            return Ok(new
            {
                status = 200,
                data = result.Value
            });
        }

        return BadRequest(new
        {
            status = 400,
            message = result.Error
        });
    }

}