using DrugStoreWebSiteData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrugStoreWebSite.Infrastructure.Persistence.Configurations;

public class ProductMedicalDetailConfiguration : IEntityTypeConfiguration<ProductMedicalDetail>
{
    public void Configure(EntityTypeBuilder<ProductMedicalDetail> builder)
    {
        builder.ToTable("ProductMedicalDetails");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Ingredients).HasMaxLength(1000);
        builder.Property(x => x.Indications).HasMaxLength(2000);
        builder.Property(x => x.Contraindications).HasMaxLength(1000);
        builder.Property(x => x.Dosage).HasMaxLength(500);
        builder.Property(x => x.SideEffects).HasMaxLength(1000);
        builder.Property(x => x.Usage).HasMaxLength(500);
    }
}