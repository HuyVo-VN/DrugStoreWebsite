namespace DrugStoreWebSiteData.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; } = true;
        public string UpdatedBy { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public ICollection<Product> Products { get; private set; } = new List<Product>();

        private Category() { }
        public Category(string name, string description)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = string.Empty;
        }

        public void UpdateDetails(string name, string description, string updatedBy)
        {
            Name = name;
            Description = description ?? string.Empty;
            UpdatedBy = updatedBy ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatus(bool isActive, string updatedBy)
        {
            IsActive = isActive;
            UpdatedBy = updatedBy ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}