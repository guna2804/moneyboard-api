using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Domain.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId);
        Task CreateAsync(RefreshToken token);
        Task UpdateAsync(RefreshToken token);
    }
}
