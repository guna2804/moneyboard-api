using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<bool> ExistsByEmailAsync(string email);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
    }
}
