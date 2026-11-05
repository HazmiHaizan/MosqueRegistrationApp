using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MosqueRegistrationApp.Services
{
    public interface ICloudStorageService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }

    public class CloudStorageService : ICloudStorageService
    {
        private readonly IConfiguration _configuration;

        public CloudStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var serviceUrl = _configuration["CloudStorage:ServiceUrl"]; // e.g., Cloudflare R2 endpoint
            var accessKey = _configuration["CloudStorage:AccessKey"];
            var secretKey = _configuration["CloudStorage:SecretKey"];
            var bucketName = _configuration["CloudStorage:BucketName"];

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true
            };

            using var client = new AmazonS3Client(accessKey, secretKey, config);
            
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = new MemoryStream(memoryStream.ToArray()),
                BucketName = bucketName,
                Key = uniqueFileName,
                ContentType = file.ContentType
            };

            var fileTransferUtility = new TransferUtility(client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            // Return the public URL to save in your database user record
            return $"{_configuration["CloudStorage:PublicUrl"]}/{uniqueFileName}";
        }
    }
}