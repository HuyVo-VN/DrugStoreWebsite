using DrugStoreWebSiteData.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.IO;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting;

namespace DrugStoreWebSiteData.Infrastructure.Services
{
    public class MinIoService : IMinIoService
    {
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MinIoService> _logger;
        private readonly IWebHostEnvironment _env;

        // Tạo sẵn một cái rổ (bucket) tên là 'reports' để chuyên chứa báo cáo
        private readonly string _bucketName = "reports";

        public MinIoService(IConfiguration configuration, ILogger<MinIoService> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;

            var endpoint = _configuration["MINIO_ENDPOINT"];
            var accessKey = _configuration["MINIO_ACCESS_KEY"];
            var secretKey = _configuration["MINIO_SECRET_KEY"];

            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();
        }

        public async Task<string> UploadFileAsync(byte[] fileData, string fileName, string contentType)
        {
            try
            {
                var bucketExistArgs = new BucketExistsArgs().WithBucket(_bucketName);
                bool found = await _minioClient.BucketExistsAsync(bucketExistArgs);
                if (!found)
                {
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));

                    _logger.LogInformation($"A new Bucket has been created.: {_bucketName}");
                }

                var policy = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{_bucketName}/*""]}}]}}";
                await _minioClient.SetPolicyAsync(new SetPolicyArgs().WithBucket(_bucketName).WithPolicy(policy));

                using var stream = new MemoryStream(fileData);
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);
                _logger.LogInformation($"File uploaded successfully: {fileName}");

                // TẠO LINK TRỰC TIẾP
                string publicHost = "https://drugstore-huyvo.duckdns.org/minio-files";
                string finalUrl = $"{publicHost}/{_bucketName}/{fileName}";

                return finalUrl;
            }
            catch (MinioException e)
            {
                _logger.LogError($"Error MinIO: {e.Message}");
                throw new Exception("The internal warehousing system is experiencing problems.");
            }
        }
    }
}
