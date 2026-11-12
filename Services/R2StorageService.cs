using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MosqueRegistrationApp.Services
{
    public interface IR2StorageService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }

    public class R2StorageService : IR2StorageService
    {
        private readonly string _bucketName;
        private readonly string _publicUrl;
        private readonly IAmazonS3 _s3Client;

        public R2StorageService(IConfiguration configuration)
        {
            // Read from configuration or fallback directly to Render environment variables
            _bucketName = configuration["CloudflareR2__BucketName"] 
                       ?? configuration["CloudflareR2:BucketName"] 
                       ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_BUCKET_NAME");

            _publicUrl = (configuration["CloudflareR2__PublicUrl"] 
                       ?? configuration["CloudflareR2:PublicUrl"] 
                       ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_PUBLIC_URL"))?.TrimEnd('/');

            var accessKey = configuration["CloudflareR2__AccessKey"] 
                         ?? configuration["CloudflareR2:AccessKey"] 
                         ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_ACCESS_KEY");

            var secretKey = configuration["CloudflareR2__SecretKey"] 
                         ?? configuration["CloudflareR2:SecretKey"] 
                         ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_SECRET_KEY");

            var serviceUrl = configuration["CloudflareR2__ServiceUrl"] 
                          ?? configuration["CloudflareR2:ServiceUrl"] 
                          ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_SERVICE_URL");

            if (string.IsNullOrEmpty(serviceUrl))
            {
                throw new InvalidOperationException("R2 ServiceURL is missing. Please check your Render environment variables.");
            }

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            using (var stream = file.OpenReadStream())
            {
                var fileTransferUtility = new TransferUtility(_s3Client);
                
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = fileName,
                    BucketName = _bucketName,
                    ContentType = file.ContentType,
                    DisablePayloadSigning = true 
                };

                await fileTransferUtility.UploadAsync(uploadRequest);
            }

            return $"{_publicUrl}/{fileName}";
        }
    }
}