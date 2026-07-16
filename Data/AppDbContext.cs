using Microsoft.EntityFrameworkCore;
using ProductApi.Models;

namespace ProductApi.Data;

/// <summary>
/// Application database context using EF Core.
/// Configured with an InMemory database provider so the API runs
/// out of the box without requiring an external database server.
/// In a production scenario the provider can be swapped for
/// SQL Server / PostgreSQL / MySQL by changing the DI registration.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rt => rt.Token).IsUnique();
        });

        // Configure Product entity - store Images as a JSON column.
        // EF Core's InMemory provider will serialize the list automatically.
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Title).IsRequired();
            entity.Property(p => p.Category).IsRequired();
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });
    }
}
