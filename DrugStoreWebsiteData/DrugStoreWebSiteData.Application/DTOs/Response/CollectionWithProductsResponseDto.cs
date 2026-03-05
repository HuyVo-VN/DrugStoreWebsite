namespace DrugStoreWebSiteData.Application.DTOs.Response
{
    public class CollectionWithProductsResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public bool ShowOnHomePage { get; set; }
        public bool IsActive { get; set; }

        public List<ProductResponseDto> Products { get; set; } = new List<ProductResponseDto>();
        public List<Guid> ProductIds { get; set; } = new List<Guid>();
    }
}