using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly JwtOptions _jwt;

    private sealed record JwtOptions(string Issuer, string Audience, string Key);

    public AuthService(AppDbContext db, IConfiguration config, IBCryptService bcrypt, ITokenService token)
    {
        _db = db;
        _config = config;
        _bcrypt = bcrypt;
        _token = token;

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

        var user = new User(dto.Email, dto.Name, _bcrypt.HashPassword(dto.Password));
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new AuthResponseDto(user.Email, user.FullName, GenerateUserToken(user));
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user is null || !_bcrypt.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthResponseDto(user.Email, user.FullName, GenerateUserToken(user));
    }

    private string GenerateUserToken(User user) =>
        _token.GenerateToken(_jwt.Issuer, _jwt.Audience, _jwt.Key, user);
}