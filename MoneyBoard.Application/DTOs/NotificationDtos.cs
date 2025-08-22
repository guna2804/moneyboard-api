using MoneyBoard.Domain.Enums;
using System;

namespace MoneyBoard.Application.DTOs
{
    public class NotificationDto
    {
        public Guid LoanId { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class NotificationDetailsDto
    {
        public Guid Id { get; set; }
        public Guid LoanId { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}