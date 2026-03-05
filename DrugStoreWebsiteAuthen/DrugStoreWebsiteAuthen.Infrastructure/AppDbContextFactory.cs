using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;

namespace DrugStoreWebsiteAuthen.Infrastructure
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var connectionString = "Server=DESKTOP-ME1OU3E\\HUYVO;Database=DrugStoreAuthDB;Trusted_Connection=True;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly("DrugStoreWebsiteAuthen.Infrastructure")
            );
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
