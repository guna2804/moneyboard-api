using MoneyBoard.Domain.Common;
using PactPal.Domain.Enums;

namespace MoneyBoard.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; private set; }
        public string FullName { get; set; }
        public string PasswordHash { get; private set; }
        public bool EnableEmailNotifications { get; set; } = true;
        public string Role { get; set; }
        public string Timezone { get; set; }
        public ICollection<Loan> LoansGiven { get; set; }
        public ICollection<Loan> LoansTaken { get; set; }

        protected User()
        { }

        public User(string email, string fullName)
        {
            Id = Guid.NewGuid();
            Email = email;
            FullName = fullName;
            LoansGiven = new List<Loan>();
            LoansTaken = new List<Loan>();
        }
    }
}