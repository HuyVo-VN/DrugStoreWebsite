using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs.Response;

public class CategoryResponseDto
{
    public CategoryResponseDto()
    {
    }
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CategoryResponseDto mapToCategoryDto(Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            UpdatedAt = category.UpdatedAt
        };
    }
}