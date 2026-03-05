using DrugStoreWebSiteData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrugStoreWebSite.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Stock)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.HasOne(p => p.Category) // Each Product has one Category
            .WithMany(c => c.Products) // Each Category has many Products
            .HasForeignKey(p => p.CategoryId) // Foreign key in Product table
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

        builder.HasOne(p => p.MedicalDetail)
            .WithOne(m => m.Product)
            .HasForeignKey<ProductMedicalDetail>(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}