using Microsoft.EntityFrameworkCore;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Infrastructure.Data
{
    public class UserRepository(AppDbContext context) : IUserRepository
    {
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await context.Users.AnyAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task CreateAsync(User user)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            user.SetUpdated();
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }
    }
}
