using Microsoft.EntityFrameworkCore;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Infrastructure.Data
{
    public class PasswordResetTokenRepository(AppDbContext context) : IPasswordResetTokenRepository
    {
        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
        {
            return await context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsDeleted);
        }

        public async Task<IEnumerable<PasswordResetToken>> GetExpiredTokensByEmailAsync(string email)
        {
            return await context.PasswordResetTokens
                .Where(t => t.Email == email && !t.IsValid && !t.IsDeleted)
                .ToListAsync();
        }

        public async Task CreateAsync(PasswordResetToken token)
        {
            context.PasswordResetTokens.Add(token);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PasswordResetToken token)
        {
            token.SetUpdated();
            context.PasswordResetTokens.Update(token);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(PasswordResetToken token)
        {
            context.PasswordResetTokens.Remove(token);
            await context.SaveChangesAsync();
        }
    }
}
