using DrugStoreWebSiteData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrugStoreWebSiteData.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oi => oi.ProductImage)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(oi => oi.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.Quantity)
            .IsRequired();
            
        builder.HasOne(oi => oi.Product)
            .WithMany() 
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict); 
    }
}