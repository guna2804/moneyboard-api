using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IConfiguration _config;
    private readonly IBCryptService _bcrypt;
    private readonly ITokenService _token;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtOptions _jwt;

    private sealed record JwtOptions(string Issuer, string Audience, string Key);

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IConfiguration config,
        IBCryptService bcrypt,
        ITokenService token,
        IEmailService emailService,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
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
        if (await _userRepository.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException("User with this email already exists.");

        var user = _mapper.Map<User>(dto);
        user.PasswordHash = _bcrypt.HashPassword(dto.Password) ?? "";

        await _userRepository.CreateAsync(user);

        // Generate tokens
        var accessToken = GenerateUserToken(user);
        var refreshTokenValue = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days

        var refreshToken = new RefreshToken(user.Id, refreshTokenValue, refreshTokenExpiry);
        await _refreshTokenRepository.CreateAsync(refreshToken);

        _logger.LogInformation("User {UserId} registered successfully", user.Id);

        return _mapper.Map<AuthResponseDto>(
            user,
            opt =>
            {
                opt.Items["Token"] = accessToken;
                opt.Items["RefreshToken"] = refreshTokenValue;
            }
        );
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user is null || !_bcrypt.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        // Generate tokens
        var accessToken = GenerateUserToken(user);
        var refreshTokenValue = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days

        var refreshToken = new RefreshToken(user.Id, refreshTokenValue, refreshTokenExpiry);
        await _refreshTokenRepository.CreateAsync(refreshToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return _mapper.Map<AuthResponseDto>(
            user,
            opt =>
            {
                opt.Items["Token"] = accessToken;
                opt.Items["RefreshToken"] = refreshTokenValue;
            }
        );
    }

    private string GenerateUserToken(User user) =>
        _token.GenerateToken(_jwt.Issuer, _jwt.Audience, _jwt.Key, user);

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(dto.RefreshToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            _logger.LogWarning("Invalid refresh token attempt");
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // Revoke the old refresh token
        refreshToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(refreshToken);

        // Generate new tokens
        var newAccessToken = GenerateUserToken(refreshToken.User);
        var newRefreshTokenValue = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days

        var newRefreshToken = new RefreshToken(
            refreshToken.UserId,
            newRefreshTokenValue,
            refreshTokenExpiry
        );

        await _refreshTokenRepository.CreateAsync(newRefreshToken);

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
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null) return; // Silently return for security

        // Clean up expired tokens for this user
        var expiredTokens = await _passwordResetTokenRepository.GetExpiredTokensByEmailAsync(dto.Email);

        // Delete expired tokens to keep the database clean
        foreach (var expiredToken in expiredTokens)
        {
            await _passwordResetTokenRepository.DeleteAsync(expiredToken);
        }

        // Generate secure reset token
        var resetTokenValue = Guid.NewGuid().ToString();
        var tokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour expiry

        var resetToken = new PasswordResetToken(dto.Email, resetTokenValue, tokenExpiry);
        await _passwordResetTokenRepository.CreateAsync(resetToken);

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
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null) throw new InvalidOperationException("Invalid request.");

        // Find and validate reset token
        var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(dto.Token);

        if (resetToken == null || resetToken.Email != dto.Email)
        {
            _logger.LogWarning("Invalid password reset attempt for email {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid or expired reset token.");
        }

        // Mark token as used
        resetToken.MarkAsUsed();
        await _passwordResetTokenRepository.UpdateAsync(resetToken);

        // Update password
        user.PasswordHash = _bcrypt.HashPassword(dto.NewPassword) ?? "";
        await _userRepository.UpdateAsync(user);

        // Revoke all refresh tokens for security
        var userRefreshTokens = await _refreshTokenRepository.GetActiveTokensByUserIdAsync(user.Id);

        foreach (var token in userRefreshTokens)
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token);
        }

        _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
        if (user == null) throw new InvalidOperationException("User not found.");

        // Verify current password
        if (!_bcrypt.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Invalid current password attempt for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        // Update password
        user.PasswordHash = _bcrypt.HashPassword(dto.NewPassword) ?? "";
        await _userRepository.UpdateAsync(user);

        // Revoke all refresh tokens for security
        var userRefreshTokens = await _refreshTokenRepository.GetActiveTokensByUserIdAsync(user.Id);

        foreach (var token in userRefreshTokens)
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token);
        }

        _logger.LogInformation("Password changed successfully for user {UserId}", user.Id);
    }
}
