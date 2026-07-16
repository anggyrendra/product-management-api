using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductApi.DTOs;

/// <summary>
/// DTO for the register endpoint. Validates that password and
/// password_confirmation match.
/// </summary>
public class RegisterDto
{
    [Required(ErrorMessage = "username is required")]
    [MinLength(3, ErrorMessage = "username must be at least 3 characters")]
    [MaxLength(100, ErrorMessage = "username must be at most 100 characters")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "password is required")]
    [MinLength(6, ErrorMessage = "password must be at least 6 characters")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "password_confirmation is required")]
    [Compare("Password", ErrorMessage = "password and password_confirmation do not match")]
    [JsonPropertyName("password_confirmation")]
    public string PasswordConfirmation { get; set; } = string.Empty;
}

/// <summary>
/// DTO for the login endpoint.
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "username is required")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "password is required")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Returned to the client after successful authentication.
/// Contains both the short-lived authentication token and a refresh token.
/// </summary>
public class AuthResponseDto
{
    [JsonPropertyName("authentication_token")]
    public string AuthenticationToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>
/// DTO for refreshing an expired authentication token using a refresh token.
/// </summary>
public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "refresh_token is required")]
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}
