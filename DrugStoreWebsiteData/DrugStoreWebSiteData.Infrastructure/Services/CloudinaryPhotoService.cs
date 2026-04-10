using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DrugStoreWebSiteData.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DrugStoreWebSiteData.Infrastructure.Services
{
    public class CloudinaryPhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryPhotoService(IConfiguration config)
        {
            var acc = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> AddPhotoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadResult = new ImageUploadResult();
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face"),
                    Folder = "DrugStoreWebsite"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null) throw new Exception(uploadResult.Error.Message);

            return uploadResult.SecureUrl.AbsoluteUri;
        }

        // Hàm xóa ảnh trên Cloudinary
        public async Task<bool> DeletePhotoAsync(string imageUrl)
        {
            try
            {
                // Tách lấy PublicId từ cái URL để xóa
                var uri = new Uri(imageUrl);
                var fileName = uri.Segments.Last().Split('.')[0];
                var publicId = $"DrugStoreWebsite/{fileName}";

                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}
