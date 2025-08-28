using MoneyBoard.Domain.Common;

namespace MoneyBoard.Domain.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public string Token { get; private set; } = default!;
        public string Email { get; private set; } = default!;
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }

        protected PasswordResetToken()
        { }

        public PasswordResetToken(string email, string token, DateTime expiresAt)
        {
            Id = Guid.NewGuid();
            Email = email;
            Token = token;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
            IsUsed = false;
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
        }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsValid => !IsUsed && !IsExpired;
    }
}