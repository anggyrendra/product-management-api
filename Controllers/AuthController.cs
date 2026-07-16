using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductApi.DTOs;
using ProductApi.Services;

namespace ProductApi.Controllers;

/// <summary>
/// Handles user registration and authentication. Both endpoints are
/// rate limited to 3 requests per 60 seconds per client to mitigate
/// brute-force attacks.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registers a new user. Requires username, password and matching
    /// password_confirmation. Rate limited: 3 requests / 60 seconds.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth-policy")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<AuthResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiErrorResponse))]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var (success, error, response) = await _authService.RegisterAsync(dto);
        if (!success)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                Message = error ?? "Registration failed"
            });
        }

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "User registered successfully",
            Data = response
        });
    }

    /// <summary>
    /// Authenticates a user and returns an authentication_token and a
    /// refresh_token. Rate limited: 3 requests / 60 seconds.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-policy")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<AuthResponseDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiErrorResponse))]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ApiErrorResponse))]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (success, error, response) = await _authService.LoginAsync(dto);
        if (!success)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                Message = error ?? "Invalid credentials"
            });
        }

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "Login successful",
            Data = response
        });
    }

    /// <summary>
    /// Exchanges a valid refresh_token for a new authentication_token
    /// and a rotated refresh_token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<AuthResponseDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiErrorResponse))]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var response = await _tokenService.RefreshTokensAsync(dto.RefreshToken);
        if (response == null)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            });
        }

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "Token refreshed successfully",
            Data = response
        });
    }
}
