using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models;

/// <summary>
/// Stores a refresh token issued to a user so they can obtain new
/// authentication tokens without logging in again. Each token is
/// single-use and has an expiry date.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Marks whether this refresh token has already been used to mint a new token.
    /// </summary>
    public bool IsRevoked { get; set; }
}
