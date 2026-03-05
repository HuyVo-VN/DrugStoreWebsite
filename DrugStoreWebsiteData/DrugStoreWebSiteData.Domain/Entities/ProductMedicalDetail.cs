using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrugStoreWebSiteData.Domain.Entities;

public class ProductMedicalDetail
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string? Ingredients { get; set; }
    public string? Indications { get; set; }
    public string? Contraindications { get; set; }
    public string? Dosage { get; set; }
    public string? SideEffects { get; set; }
    public string? Usage { get; set; }

    // Constructor
    public ProductMedicalDetail() { }
}