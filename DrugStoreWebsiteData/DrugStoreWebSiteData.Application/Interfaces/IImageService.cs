using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface IImageService
{
    /// <summary>
    /// Save file, resize and return imageURL
    /// </summary>
    /// <param name="imageFile">Image file from request</param>
    /// <returns>Public URL (exx: /images/products/panadol.png)</returns>
    Task<string> SaveImageAsync(Microsoft.AspNetCore.Http.IFormFile file, string folderName = "products");

    /// <summary>
    /// Delete image file
    /// </summary>
    /// <param name="imageUrl">Public URL</param>
    void DeleteImage(string imageUrl);
}