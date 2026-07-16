using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models;

/// <summary>
/// Represents a product entity with full audit tracking information.
/// Includes fields for creation and update metadata (who, when, by whom).
/// </summary>
public class Product
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// List of image URLs associated with the product.
    /// Must contain at least one entry (validation handled in DTO).
    /// </summary>
    public List<string> Images { get; set; } = new();

    // ---- Audit fields ----
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public string UpdatedById { get; set; } = string.Empty;
}
