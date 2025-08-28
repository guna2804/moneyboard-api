using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Domain.Repositories
{
    public interface IPasswordResetTokenRepository
    {
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task<IEnumerable<PasswordResetToken>> GetExpiredTokensByEmailAsync(string email);
    Task CreateAsync(PasswordResetToken token);
    Task UpdateAsync(PasswordResetToken token);
    Task DeleteAsync(PasswordResetToken token);
    }
}
