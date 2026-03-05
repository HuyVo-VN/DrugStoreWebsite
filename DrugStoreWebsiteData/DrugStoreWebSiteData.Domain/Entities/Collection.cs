namespace DrugStoreWebSiteData.Domain.Entities
{
    public class Collection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public bool ShowOnHomePage { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;

        // Relationship
        public ICollection<ProductCollection> ProductCollections { get; set; } = new List<ProductCollection>();
    }
}