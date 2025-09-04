namespace MoneyBoard.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }
        public bool IsDeleted { get; protected set; } = false;

        protected BaseEntity()
        {
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            IsDeleted = false;
        }

        public void SetUpdated(Guid? updatedBy = null)
        {
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        }

        public void SetCreated(Guid? createdBy = null)
        {
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        }

        public void SetDeleted(Guid? deletedBy = null)
        {
            IsDeleted = true;
            SetUpdated(deletedBy);
        }

        public void Restore(Guid? updatedBy = null)
        {
            IsDeleted = false;
            SetUpdated(updatedBy);
        }
    }
}