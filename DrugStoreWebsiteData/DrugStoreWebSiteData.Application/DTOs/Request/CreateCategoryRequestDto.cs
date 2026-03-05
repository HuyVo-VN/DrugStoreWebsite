using System.ComponentModel.DataAnnotations;
using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs.Request;

public class CreateCategoryRequestDto
{
    public CreateCategoryRequestDto()
    {
    }
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = null!;

    public Category mapToCategory()
    {
        return new Category(Name, Description);
    }
}