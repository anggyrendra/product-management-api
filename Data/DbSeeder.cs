using Microsoft.EntityFrameworkCore;
using ProductApi.Models;

namespace ProductApi.Data;

/// <summary>
/// Provides extension methods to seed the database with initial
/// sample products so the API returns data on first run.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Make sure the database is created (InMemory provider).
        await context.Database.EnsureCreatedAsync();

        // Only seed products if the table is empty.
        if (await context.Products.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var seedUser = "System";
        var seedUserId = "0";

        var products = new List<Product>
        {
            new()
            {
                Title = "Awesome T-Shirt",
                Price = 99.99m,
                Description = "High-quality cotton t-shirt",
                Category = "Clothes",
                Images = new List<string> { "https://placeimg.com/640/480/any" },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            },
            new()
            {
                Title = "Running Shoes Pro",
                Price = 149.50m,
                Description = "Lightweight running shoes with extra cushioning",
                Category = "Shoes",
                Images = new List<string>
                {
                    "https://placeimg.com/640/480/any",
                    "https://placeimg.com/640/480/tech"
                },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            },
            new()
            {
                Title = "Wireless Headphones",
                Price = 199.00m,
                Description = "Noise-cancelling over-ear wireless headphones",
                Category = "Electronics",
                Images = new List<string> { "https://placeimg.com/640/480/tech" },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            },
            new()
            {
                Title = "Smart Watch Series 5",
                Price = 299.99m,
                Description = "Fitness tracking and notifications on your wrist",
                Category = "Electronics",
                Images = new List<string>
                {
                    "https://placeimg.com/640/480/tech",
                    "https://placeimg.com/640/480/any"
                },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            },
            new()
            {
                Title = "Denim Jacket",
                Price = 79.99m,
                Description = "Classic blue denim jacket, unisex fit",
                Category = "Clothes",
                Images = new List<string> { "https://placeimg.com/640/480/any" },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            },
            new()
            {
                Title = "Leather Wallet",
                Price = 39.99m,
                Description = "Genuine leather bifold wallet with RFID protection",
                Category = "Accessories",
                Images = new List<string> { "https://placeimg.com/640/480/any" },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            },
            new()
            {
                Title = "Coffee Mug Set",
                Price = 24.99m,
                Description = "Set of 4 ceramic coffee mugs, dishwasher safe",
                Category = "Home",
                Images = new List<string> { "https://placeimg.com/640/480/any" },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            },
            new()
            {
                Title = "Gaming Keyboard RGB",
                Price = 89.99m,
                Description = "Mechanical gaming keyboard with RGB backlight",
                Category = "Electronics",
                Images = new List<string> { "https://placeimg.com/640/480/tech" },
                CreatedAt = now,
                CreatedBy = seedUser,
                CreatedById = seedUserId,
                UpdatedAt = now,
                UpdatedBy = seedUser,
                UpdatedById = seedUserId
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
