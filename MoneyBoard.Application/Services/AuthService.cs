using AutoMapper;
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
    private readonly IMapper _mapper;   // 🔹 Add AutoMapper
    private readonly JwtOptions _jwt;

    private sealed record JwtOptions(string Issuer, string Audience, string Key);

    public AuthService(
        AppDbContext db,
        IConfiguration config,
        IBCryptService bcrypt,
        ITokenService token,
        IMapper mapper)   // 🔹 Add IMapper here
    {
        _db = db;
        _config = config;
        _bcrypt = bcrypt;
        _token = token;
        _mapper = mapper;   // 🔹 Assign

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

        var authResponse = _mapper.Map<AuthResponseDto>(
            user,
            opt => opt.Items["Token"] = GenerateUserToken(user) // pass token through context
        );

        return authResponse;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user is null || !_bcrypt.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        // 🔹 Use mapping profile instead of manual construction
        return _mapper.Map<AuthResponseDto>(
            user,
            opt => opt.Items["Token"] = GenerateUserToken(user)
        );
    }

    private string GenerateUserToken(User user) =>
        _token.GenerateToken(_jwt.Issuer, _jwt.Audience, _jwt.Key, user);
}