using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductApi.DTOs;
using ProductApi.Services;

namespace ProductApi.Controllers;

/// <summary>
/// REST API for product management.
/// - GET endpoints are public.
/// - POST / PUT / DELETE require a valid JWT (authorization) and are
///   rate limited to 1 request per 5 seconds per client.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductsService _productsService;
    private readonly ICurrentUserResolver _currentUserResolver;

    public ProductsController(
        IProductsService productsService,
        ICurrentUserResolver currentUserResolver)
    {
        _productsService = productsService;
        _currentUserResolver = currentUserResolver;
    }

    /// <summary>
    /// Returns all products. Supports optional filtering and pagination:
    /// ?search=keyword, ?category=Clothes, ?limit=10&page=2.
    /// Results are cached in-memory for a short period.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<ProductResponseDto>))]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        var result = await _productsService.GetProductsAsync(search, category, page, limit);
        return Ok(result);
    }

    /// <summary>
    /// Returns the details of a single product. Returns 404 if not found.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ProductResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiErrorResponse))]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productsService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound(new ApiErrorResponse
            {
                Success = false,
                Message = $"Product with id {id} not found"
            });
        }

        return Ok(new ApiResponse<ProductResponseDto>
        {
            Success = true,
            Message = "Product found",
            Data = product
        });
    }

    /// <summary>
    /// Creates a new product. Requires authorization. Validates that
    /// title, price, category and at least one image are provided.
    /// Rate limited: 1 request / 5 seconds.
    /// </summary>
    [HttpPost]
    [Authorize]
    [EnableRateLimiting("write-policy")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse<ProductResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiErrorResponse))]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        var currentUser = _currentUserResolver.Resolve(User);
        if (currentUser == null)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                Message = "User could not be identified from token"
            });
        }

        var created = await _productsService.CreateProductAsync(dto, currentUser);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            new ApiResponse<ProductResponseDto>
            {
                Success = true,
                Message = "Product created successfully",
                Data = created
            });
    }

    /// <summary>
    /// Updates an existing product by id. Requires authorization.
    /// Returns 404 if the product does not exist.
    /// Rate limited: 1 request / 5 seconds.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [EnableRateLimiting("write-policy")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ProductResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiErrorResponse))]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
    {
        var currentUser = _currentUserResolver.Resolve(User);
        if (currentUser == null)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                Message = "User could not be identified from token"
            });
        }

        var (found, updated) = await _productsService.UpdateProductAsync(id, dto, currentUser);
        if (!found)
        {
            return NotFound(new ApiErrorResponse
            {
                Success = false,
                Message = $"Product with id {id} not found"
            });
        }

        return Ok(new ApiResponse<ProductResponseDto>
        {
            Success = true,
            Message = "Product updated successfully",
            Data = updated
        });
    }

    /// <summary>
    /// Deletes a product by id. Requires authorization.
    /// Returns 404 if the product does not exist.
    /// Rate limited: 1 request / 5 seconds.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [EnableRateLimiting("write-policy")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiErrorResponse))]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = _currentUserResolver.Resolve(User);
        if (currentUser == null)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                Message = "User could not be identified from token"
            });
        }

        var deleted = await _productsService.DeleteProductAsync(id);
        if (!deleted)
        {
            return NotFound(new ApiErrorResponse
            {
                Success = false,
                Message = $"Product with id {id} not found"
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Product deleted successfully"
        });
    }
}
