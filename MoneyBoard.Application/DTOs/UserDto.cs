using System;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool EnableEmailNotifications { get; set; } = true;
        public string Role { get; set; } = RolesType.User.ToString();
        public string Timezone { get; set; } = "UTC";
    }
}
