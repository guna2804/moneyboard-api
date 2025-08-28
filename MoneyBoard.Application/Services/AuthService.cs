using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Infrastructure.Data;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IBCryptService _bcrypt;
    private readonly ITokenService _token;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtOptions _jwt;

    private sealed record JwtOptions(string Issuer, string Audience, string Key);

    public AuthService(
        AppDbContext db,
        IConfiguration config,
        IBCryptService bcrypt,
        ITokenService token,
        IEmailService emailService,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _bcrypt = bcrypt;
        _token = token;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;

        _jwt = new JwtOptions(
            config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer"),
            config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience"),
            config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key")
        );
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            throw new InvalidOperationException("User with this email already exists.");

        var user = _mapper.Map<User>(dto);
        user.PasswordHash = _bcrypt.HashPassword(dto.Password) ?? "";

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Generate tokens
        var accessToken = GenerateUserToken(user);
        var refreshTokenValue = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days

        var refreshToken = new RefreshToken(user.Id, refreshTokenValue, refreshTokenExpiry);
        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} registered successfully", user.Id);

        return _mapper.Map<AuthResponseDto>(
            user,
            opt => {
                opt.Items["Token"] = accessToken;
                opt.Items["RefreshToken"] = refreshTokenValue;
            }
        );
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user is null || !_bcrypt.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        // Generate tokens
        var accessToken = GenerateUserToken(user);
        var refreshTokenValue = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days

        var refreshToken = new RefreshToken(user.Id, refreshTokenValue, refreshTokenExpiry);
        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return _mapper.Map<AuthResponseDto>(
            user,
            opt => {
                opt.Items["Token"] = accessToken;
                opt.Items["RefreshToken"] = refreshTokenValue;
            }
        );
    }

    private string GenerateUserToken(User user) =>
        _token.GenerateToken(_jwt.Issuer, _jwt.Audience, _jwt.Key, user);

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var refreshToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && rt.IsActive, ct);

        if (refreshToken == null)
        {
            _logger.LogWarning("Invalid refresh token attempt");
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // Revoke the old refresh token
        refreshToken.Revoke();

        // Generate new tokens
        var newAccessToken = GenerateUserToken(refreshToken.User);
        var newRefreshTokenValue = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days

        var newRefreshToken = new RefreshToken(
            refreshToken.UserId,
            newRefreshTokenValue,
            refreshTokenExpiry
        );

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Tokens refreshed for user {UserId}", refreshToken.UserId);

        return new AuthResponseDto(
            refreshToken.User.Email,
            refreshToken.User.FullName,
            newAccessToken,
            newRefreshTokenValue
        );
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (user == null) return; // Silently return for security

        // Clean up expired tokens for this user
        var expiredTokens = await _db.PasswordResetTokens
            .Where(t => t.Email == dto.Email && !t.IsValid)
            .ToListAsync(ct);

        _db.PasswordResetTokens.RemoveRange(expiredTokens);

        // Generate secure reset token
        var resetTokenValue = Guid.NewGuid().ToString();
        var tokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour expiry

        var resetToken = new PasswordResetToken(dto.Email, resetTokenValue, tokenExpiry);
        _db.PasswordResetTokens.Add(resetToken);

        await _db.SaveChangesAsync(ct);

        // Send email
        try
        {
            await _emailService.SendPasswordResetEmailAsync(dto.Email, resetTokenValue, ct);
            _logger.LogInformation("Password reset email sent to {Email}", dto.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", dto.Email);
            // Don't throw - we don't want to reveal if email exists or not
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (user == null) throw new InvalidOperationException("Invalid request.");

        // Find and validate reset token
        var resetToken = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Email == dto.Email && t.Token == dto.Token && t.IsValid, ct);

        if (resetToken == null)
        {
            _logger.LogWarning("Invalid password reset attempt for email {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid or expired reset token.");
        }

        // Mark token as used
        resetToken.MarkAsUsed();

        // Update password
        user.PasswordHash = _bcrypt.HashPassword(dto.NewPassword) ?? "";

        // Revoke all refresh tokens for security
        var userRefreshTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.IsActive)
            .ToListAsync(ct);

        foreach (var token in userRefreshTokens)
        {
            token.Revoke();
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);
    }
}
