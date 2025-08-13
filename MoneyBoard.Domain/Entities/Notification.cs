using MoneyBoard.Domain.Common;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid LoanId { get; set; }
        public Loan Loan { get; set; }
        public NotificationType Type { get; set; } // Due, Overdue, Reminder, etc.
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }

        public Notification()
        { Timestamp = DateTime.UtcNow; }
    }
}