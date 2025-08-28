namespace MoneyBoard.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }
        public bool IsDeleted { get; protected set; } = false;
        public byte[] RowVersion { get; set; } = [];

        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsDeleted = false;
        }

        public void SetUpdated(Guid? updatedBy = null)
        {
            UpdatedAt = DateTime.UtcNow;
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