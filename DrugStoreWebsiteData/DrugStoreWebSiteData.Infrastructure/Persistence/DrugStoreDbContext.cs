using System.Reflection;
using DrugStoreWebSiteData.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DrugStoreWebSiteData.Infrastructure.Persistence;

public class DrugStoreDbContext : DbContext
{
    public DrugStoreDbContext(DbContextOptions<DrugStoreDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ProductMedicalDetail> ProductMedicalDetails { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<ProductCollection> ProductCollections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ProductCollection>()
        .HasKey(pc => new { pc.ProductId, pc.CollectionId });
    }
}