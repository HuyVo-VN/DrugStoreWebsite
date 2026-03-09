namespace DrugStoreWebSiteData.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }
        public int Stock { get; private set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; private set; } = true;
        public string UpdatedBy { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public int DiscountPercent { get; set; } = 0;
        public int SoldQuantity { get; set; } = 0;
        public DateTime? DiscountEndDate { get; set; } 
        public int SaleStock { get; private set; } = 0;
        public int SaleSold { get; private set; } = 0;

        public ProductMedicalDetail? MedicalDetail { get; set; }

        // Foreign Key
        public Guid CategoryId { get; private set; }

        public ICollection<ProductCollection> ProductCollections { get; set; } = new List<ProductCollection>();

        // Navigation property
        public Category Category { get; private set; } = null!;

        private Product() { }

        public Product(string name, string description, decimal price, int stock, Guid categoryId, int discountPercent, DateTime? discountEndDate,int saleStock)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description ?? string.Empty;
            Price = price;
            Stock = stock;
            CategoryId = categoryId;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = string.Empty;
            DiscountPercent = discountPercent;
            DiscountEndDate = discountEndDate;
            SaleStock = saleStock;
        }

        public void UpdateStatus(bool status, string updatedBy, DateTime updatedAt)
        {
            IsActive = status;
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;
        }


        /// <summary>
        /// Updates the product details.
        /// </summary>
        public void UpdateDetails(string name, string description, decimal price, int stock, string imageUrl, Guid categoryId, int discountPercent, DateTime? discountEndDate, int saleStock)
        {
            Name = name;
            Description = description ?? string.Empty;
            Price = price;
            Stock = stock;
            ImageUrl = imageUrl ?? string.Empty;
            CategoryId = categoryId;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = string.Empty;
            DiscountPercent = discountPercent;
            DiscountEndDate = discountEndDate;
            SaleStock = saleStock;
        }

        public void DecreaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (Stock < quantity)
                throw new InvalidOperationException("Not enough stock.");

            Stock -= quantity;
        }

        public void IncrementSaleSold(int quantity)
        {
            SaleSold += quantity;
        }

        public void UpdateStockAndSales(int quantityBought)
        {
            Stock -= quantityBought;
            SoldQuantity += quantityBought;

            bool isFlashSaleActive = DiscountPercent > 0
                                  && DiscountEndDate.HasValue
                                  && DiscountEndDate.Value > DateTime.UtcNow
                                  && SaleSold < SaleStock;

            if (isFlashSaleActive)
            {
                SaleSold += quantityBought;
            }
        }

        public void CancelFlashSale()
        {
            DiscountPercent = 0;
            DiscountEndDate = null;
            SaleStock = 0;
            SaleSold = 0;
        }

    }
}