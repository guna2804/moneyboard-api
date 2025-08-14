using Microsoft.IdentityModel.Tokens;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MoneyBoard.Application.Services
{
    public class TokenService : ITokenService
    {
        public string GenerateToken(string issuer, string audience, string key, User authenticatedUser)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, authenticatedUser.Email),
                new(ClaimTypes.Name, authenticatedUser.FullName),
                new(ClaimTypes.Role, authenticatedUser.Role ?? "User")
            };
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}