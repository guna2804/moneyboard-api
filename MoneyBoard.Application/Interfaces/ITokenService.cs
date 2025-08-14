using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(string issuer, string audience, string key, User authenticatedUser);
    }
}