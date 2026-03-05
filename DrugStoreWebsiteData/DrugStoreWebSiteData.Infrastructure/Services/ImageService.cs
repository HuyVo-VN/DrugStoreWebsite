using System.Formats.Asn1;
using DrugStoreWebSiteData.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DrugStoreWebSiteData.Infrastructure.Services;

public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ImageService> _logger;
    private const int DefaultImageWidth = 800;

    public ImageService(
        IWebHostEnvironment webHostEnvironment,
        ILogger<ImageService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string folderName = "products")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.");

        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", folderName);

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/images/{folderName}/{uniqueFileName}";
    }

    public void DeleteImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;
        
        // Change URL to pysical (ex: /images/products/file.jpg)
        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}