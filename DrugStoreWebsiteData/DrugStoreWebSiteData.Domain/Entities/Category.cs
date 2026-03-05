namespace DrugStoreWebSiteData.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }

        // Navigation property
        public ICollection<Product> Products { get; private set; } = new List<Product>();

        // Using a private constructor for EF Core
        private Category() { }

        public Category(string name, string description)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description ?? string.Empty;
        }
    }
}