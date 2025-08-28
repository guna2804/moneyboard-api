using MoneyBoard.Domain.Common;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; private set; } = default!;
        public string FullName { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public bool EnableEmailNotifications { get; set; } = true;
        public string Role { get; set; } = RolesType.User.ToString(); // default
        public string Timezone { get; set; } = "UTC";
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        protected User()
        { }

        public User(string email, string fullName, string passwordHash, RolesType role = RolesType.User)
        {
            Id = Guid.NewGuid();
            Email = email;
            FullName = fullName;
            PasswordHash = passwordHash;
            Role = role.ToString();
        }
    }
}
