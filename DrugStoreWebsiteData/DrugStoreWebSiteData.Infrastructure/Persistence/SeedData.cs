using DrugStoreWebSiteData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DrugStoreWebSiteData.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DrugStoreDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DrugStoreDbContext>>();

        try
        {
            if (context.Database.IsSqlServer())
            {
                await context.Database.MigrateAsync();
            }

            // If data already exists, skip seeding
            if (await context.Products.AnyAsync()) return;

            logger.LogInformation("Seeding standardized medical reference data for RAG...");

            // 1. Create Categories
            var catAnalgesicAntipyretic = new Category(
                "Analgesics and Antipyretics",
                "Common medications used for pain relief and fever reduction"
            );

            var catGastrointestinal = new Category(
                "Gastrointestinal",
                "Medications for the treatment of gastric and intestinal disorders"
            );

            await context.Categories.AddRangeAsync(catAnalgesicAntipyretic, catGastrointestinal);
            await context.SaveChangesAsync(); // Save to generate Category IDs

            // 2. Create Products with Medical Details (Nested Medical Object)
            var products = new List<Product>();

            // --- PRODUCT 1: PANADOL ---
            var panadol = new Product(
                "Panadol Extra",
                "An effective analgesic and antipyretic formulation containing caffeine to enhance alertness.",
                150000,
                100,
                catAnalgesicAntipyretic.Id,
                0,
               null,
                0
            )
            {
                ImageUrl = "/images/products/panadol.webp"
            };

            // Attach medical-grade information (used for AI training)
            panadol.MedicalDetail = new ProductMedicalDetail
            {
                Ingredients = "Paracetamol 500 mg, Caffeine 65 mg",
                Indications = "Management of mild to moderate pain including headache, migraine, musculoskeletal pain, dysmenorrhea, sore throat, and dental pain. Also indicated for the reduction of fever.",
                Contraindications = "Severe hepatic or renal impairment. Hypersensitivity to paracetamol. Patients with glucose-6-phosphate dehydrogenase (G6PD) deficiency.",
                Dosage = "Adults (including the elderly) and adolescents aged 12 years and older: 1–2 tablets every 4 to 6 hours as needed. Maximum dose: 8 tablets within 24 hours.",
                SideEffects = "Rare cases of thrombocytopenia and hypersensitivity skin reactions such as rash or angioedema.",
                Usage = "Oral administration. May be taken with or without food."
            };
            products.Add(panadol);

            // --- PRODUCT 2: BERBERINE ---
            var berberine = new Product(
                "Berberine (Mu Xiang Formula)",
                "A herbal medicinal product indicated for intestinal infections and digestive disorders.",
                25000,
                500,
                catGastrointestinal.Id,
                0,
                null,
                0
            )
            {
                ImageUrl = "/images/products/berberin.webp"
            };

            berberine.MedicalDetail = new ProductMedicalDetail
            {
                Ingredients = "Berberine chloride, Aucklandia lappa (Mu Xiang)",
                Indications = "Treatment of diarrhea, bacillary dysentery, colitis, and gastrointestinal infections.",
                Contraindications = "Pregnancy. Known hypersensitivity to any component of the formulation.",
                Dosage = "Adults: 12–15 tablets per dose, twice daily. Pediatric dosage should be adjusted according to age, typically half the adult dose.",
                SideEffects = "Generally well tolerated. Rare adverse effects reported. Overdose may result in constipation.",
                Usage = "Oral administration. Recommended to be taken after meals."
            };
            products.Add(berberine);

            // Persist data to the database
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            logger.LogInformation("Medical reference data successfully seeded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
