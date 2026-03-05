namespace DrugStoreWebSiteData.Domain.Entities
{
    public class Banner
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0; 
        public bool IsActive { get; set; } = true;
    }
}