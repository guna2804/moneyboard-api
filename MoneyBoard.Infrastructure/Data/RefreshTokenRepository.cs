using Microsoft.EntityFrameworkCore;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Infrastructure.Data
{
    public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsDeleted);
        }

        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId)
        {
            return await context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && DateTime.UtcNow < rt.ExpiresAt && !rt.IsDeleted)
                .ToListAsync();
        }

        public async Task CreateAsync(RefreshToken token)
        {
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RefreshToken token)
        {
            token.SetUpdated();
            context.RefreshTokens.Update(token);
            await context.SaveChangesAsync();
        }
    }
}
