using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProductApi.Data;
using ProductApi.DTOs;
using ProductApi.Models;

namespace ProductApi.Services;

/// <summary>
/// Generates and validates JWT authentication tokens and refresh tokens.
/// Authentication tokens are short-lived (e.g. 15 minutes), refresh tokens
/// are longer-lived (e.g. 7 days) and stored in the database so they can
/// be revoked.
/// </summary>
public interface ITokenService
{
    (string accessToken, DateTime accessTokenExpiry) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<AuthResponseDto?> RefreshTokensAsync(string refreshToken);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public TokenService(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    /// <summary>
    /// Builds a signed JWT containing the user's id and username as claims.
    /// </summary>
    public (string accessToken, DateTime accessTokenExpiry) GenerateAccessToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "ProductApi";
        var audience = _configuration["Jwt:Audience"] ?? "ProductApi";
        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenMinutes"] ?? "15");

        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        // Ensure the key is at least 256 bits for HMAC-SHA256.
        if (keyBytes.Length < 32)
        {
            keyBytes = SHA256.HashData(keyBytes);
        }
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("user_id", user.Id.ToString())
        };

        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiry);
    }

    /// <summary>
    /// Creates a cryptographically random refresh token string.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Exchanges a valid, non-revoked refresh token for a new access token
    /// and a new refresh token (rotation). The old refresh token is revoked.
    /// </summary>
    public async Task<AuthResponseDto?> RefreshTokensAsync(string refreshToken)
    {
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored == null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            return null;

        // Rotate the refresh token: revoke the old one.
        stored.IsRevoked = true;

        var (newAccess, accessExpiry) = GenerateAccessToken(stored.User);
        var newRefresh = GenerateRefreshToken();
        var refreshExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenDays"] ?? "7");

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefresh,
            UserId = stored.User.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays),
            IsRevoked = false
        });

        await _context.SaveChangesAsync();

        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenMinutes"] ?? "15");
        return new AuthResponseDto
        {
            AuthenticationToken = newAccess,
            RefreshToken = newRefresh,
            TokenType = "Bearer",
            ExpiresIn = expiryMinutes * 60
        };
    }
}
