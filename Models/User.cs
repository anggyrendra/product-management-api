using System.ComponentModel.DataAnnotations;

namespace ProductApi.Models;

/// <summary>
/// Represents a registered user in the system.
/// Passwords are stored as BCrypt hashes, never in plain text.
/// </summary>
public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password. Never expose this in responses.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
