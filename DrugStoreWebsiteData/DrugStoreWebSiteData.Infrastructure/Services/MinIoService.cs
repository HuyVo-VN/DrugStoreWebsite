using DrugStoreWebSiteData.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.IO;
using System.Threading.Tasks;
using System;

namespace DrugStoreWebSiteData.Infrastructure.Services
{
    public class MinIoService : IMinIoService
    {
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MinIoService> _logger;

        // Tạo sẵn một cái rổ (bucket) tên là 'reports' để chuyên chứa báo cáo
        private readonly string _bucketName = "reports";

        public MinIoService(IConfiguration configuration, ILogger<MinIoService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Đọc thẳng từ file .env
            var endpoint = _configuration["MINIO_ENDPOINT"];
            var accessKey = _configuration["MINIO_ACCESS_KEY"];
            var secretKey = _configuration["MINIO_SECRET_KEY"];

            // Khởi tạo tay sai MinIO
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();
        }

        public async Task<string> UploadFileAsync(byte[] fileData, string fileName, string contentType)
        {
            try
            {
                // 1. Kiểm tra xem rổ 'reports' đã có chưa, chưa có thì tự động tạo mới
                var bucketExistArgs = new BucketExistsArgs().WithBucket(_bucketName);
                bool found = await _minioClient.BucketExistsAsync(bucketExistArgs);
                if (!found)
                {
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));

                    // Cấp quyền Public Read cho cái rổ này (Để Angular gọi link tải được file về)
                    var policy = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{_bucketName}/*""]}}]}}";
                    await _minioClient.SetPolicyAsync(new SetPolicyArgs().WithBucket(_bucketName).WithPolicy(policy));

                    _logger.LogInformation($"A new Bucket has been created.: {_bucketName}");
                }

                // 2. Chuyển mảng Byte thành Dòng chảy (Stream) và ném lên MinIO
                using var stream = new MemoryStream(fileData);
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);
                _logger.LogInformation($"File uploaded successfully: {fileName}");

                // 3. Trả về đường link để tải file (Nên dùng localhost để Frontend truy cập được từ bên ngoài)
                // Lưu ý: Cổng API là 9000, cổng giao diện MinIO là 9001
                return $"http://localhost:9000/{_bucketName}/{fileName}";
            }
            catch (MinioException e)
            {
                _logger.LogError($"Lỗi MinIO: {e.Message}");
                throw new Exception("The internal warehousing system is experiencing problems.");
            }
        }
    }
}
