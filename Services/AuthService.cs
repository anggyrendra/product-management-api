using Microsoft.EntityFrameworkCore;
using ProductApi.Data;
using ProductApi.DTOs;
using ProductApi.Models;

namespace ProductApi.Services;

/// <summary>
/// Handles the business logic for user registration and login.
/// Passwords are hashed with BCrypt and never stored or returned in plain text.
/// </summary>
public interface IAuthService
{
    Task<(bool success, string? error, AuthResponseDto? response)> RegisterAsync(RegisterDto dto);
    Task<(bool success, string? error, AuthResponseDto? response)> LoginAsync(LoginDto dto);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<(bool success, string? error, AuthResponseDto? response)> RegisterAsync(RegisterDto dto)
    {
        // Check for an existing user with the same username (case-insensitive).
        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());

        if (existing != null)
            return (false, "Username already exists", null);

        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Issue tokens immediately after registration so the user is logged in.
        var response = await IssueTokensAsync(user);
        return (true, null, response);
    }

    public async Task<(bool success, string? error, AuthResponseDto? response)> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (false, "Invalid username or password", null);

        var response = await IssueTokensAsync(user);
        return (true, null, response);
    }

    /// <summary>
    /// Generates a fresh access token + refresh token pair and persists
    /// the refresh token for later rotation.
    /// </summary>
    private async Task<AuthResponseDto> IssueTokensAsync(User user)
    {
        var (accessToken, _) = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AuthenticationToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 15 * 60
        };
    }
}
