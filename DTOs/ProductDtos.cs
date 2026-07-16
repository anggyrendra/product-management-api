using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductApi.DTOs;

/// <summary>
/// DTO used when creating a new product.
/// Carries the validation rules required by POST /api/products.
/// </summary>
public class ProductCreateDto
{
    [Required(ErrorMessage = "title is required")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "price must be greater than 0")]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "category is required")]
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "images is required")]
    [MinLength(1, ErrorMessage = "images must contain at least 1 image url")]
    [JsonPropertyName("images")]
    public List<string> Images { get; set; } = new();
}

/// <summary>
/// DTO used when editing an existing product. All fields are optional,
/// only provided fields are updated on the stored entity.
/// </summary>
public class ProductUpdateDto
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "price must be greater than 0")]
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [MinLength(1, ErrorMessage = "images must contain at least 1 image url")]
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }
}

/// <summary>
/// DTO returned to clients. Contains the full product including audit fields.
/// Field names match the snake_case format required by the test specification.
/// </summary>
public class ProductResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    public List<string> Images { get; set; } = new();

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonPropertyName("created_by_id")]
    public string CreatedById { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;

    [JsonPropertyName("updated_by_id")]
    public string UpdatedById { get; set; } = string.Empty;
}
