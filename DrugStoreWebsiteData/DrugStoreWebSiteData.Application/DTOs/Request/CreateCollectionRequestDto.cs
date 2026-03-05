using System.ComponentModel.DataAnnotations;
using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs.Request
{
    public class CreateCollectionRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool ShowOnHomePage { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;

        public List<Guid> ProductIds { get; set; } = new List<Guid>();
    }
}