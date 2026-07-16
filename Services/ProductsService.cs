using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProductApi.Data;
using ProductApi.DTOs;
using ProductApi.Models;

namespace ProductApi.Services;

/// <summary>
/// Business logic for product CRUD operations, including search,
/// category filtering, pagination and a basic in-memory cache.
/// Audit fields are populated from the authenticated user context.
/// </summary>
public interface IProductsService
{
    Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(
        string? search, string? category, int page, int limit);

    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    Task<ProductResponseDto> CreateProductAsync(ProductCreateDto dto, CurrentUser currentUser);

    Task<(bool found, ProductResponseDto? updated)> UpdateProductAsync(
        int id, ProductUpdateDto dto, CurrentUser currentUser);

    Task<bool> DeleteProductAsync(int id);

    /// <summary>
    /// Clears cached product entries. Call this after any data mutation.
    /// </summary>
    void InvalidateCache();
}

/// <summary>
/// Represents the authenticated user extracted from the JWT claims.
/// Used to populate the audit fields (created_by / updated_by).
/// </summary>
public record CurrentUser(string Id, string Name);

public class ProductsService : IProductsService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private const string ListCachePrefix = "products_list_";
    private const string ItemCachePrefix = "products_item_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public ProductsService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(
        string? search, string? category, int page, int limit)
    {
        // Clamp pagination parameters to sensible bounds.
        page = page < 1 ? 1 : page;
        limit = limit < 1 ? 10 : (limit > 100 ? 100 : limit);

        var cacheKey = $"{ListCachePrefix}{search ?? "all"}_{category ?? "all"}_{page}_{limit}";

        if (_cache.TryGetValue(cacheKey, out PaginatedResponse<ProductResponseDto>? cached)
            && cached != null)
        {
            return cached;
        }

        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(p => p.Category.ToLower() == cat);
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)limit);
        if (totalPages < 1) totalPages = 1;

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        var result = new PaginatedResponse<ProductResponseDto>
        {
            Data = items.Select(MapToDto).ToList(),
            Page = page,
            Limit = limit,
            Total = total,
            TotalPages = totalPages
        };

        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        var cacheKey = $"{ItemCachePrefix}{id}";
        if (_cache.TryGetValue(cacheKey, out ProductResponseDto? cached) && cached != null)
            return cached;

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return null;

        var dto = MapToDto(product);
        _cache.Set(cacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task<ProductResponseDto> CreateProductAsync(
        ProductCreateDto dto, CurrentUser currentUser)
    {
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Title = dto.Title,
            Price = dto.Price,
            Description = dto.Description ?? string.Empty,
            Category = dto.Category,
            Images = dto.Images,
            CreatedAt = now,
            CreatedBy = currentUser.Name,
            CreatedById = currentUser.Id,
            UpdatedAt = now,
            UpdatedBy = currentUser.Name,
            UpdatedById = currentUser.Id
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        InvalidateCache();
        return MapToDto(product);
    }

    public async Task<(bool found, ProductResponseDto? updated)> UpdateProductAsync(
        int id, ProductUpdateDto dto, CurrentUser currentUser)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return (false, null);

        if (dto.Title != null) product.Title = dto.Title;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Category != null) product.Category = dto.Category;
        if (dto.Images != null && dto.Images.Count > 0) product.Images = dto.Images;

        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = currentUser.Name;
        product.UpdatedById = currentUser.Id;

        await _context.SaveChangesAsync();

        InvalidateCache();
        return (true, MapToDto(product));
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        InvalidateCache();
        return true;
    }

    public void InvalidateCache()
    {
        // The MemoryCache does not expose prefix removal, so we track the
        // "generation" of the cache. A simpler approach for this small app
        // is to remove known keys via a small helper. Here we just compact
        // by removing the list and item caches that are most likely stale.
        // For a production app consider IDistributedCache with tagged keys.
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0);
        }
    }

    /// <summary>
    /// Maps a Product entity to its response DTO, formatting the
    /// audit timestamps as "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    private static ProductResponseDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Price = p.Price,
        Description = p.Description,
        Category = p.Category,
        Images = p.Images,
        CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        CreatedBy = p.CreatedBy,
        CreatedById = p.CreatedById,
        UpdatedAt = p.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        UpdatedBy = p.UpdatedBy,
        UpdatedById = p.UpdatedById
    };
}
