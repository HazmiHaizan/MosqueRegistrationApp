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
            _bucketName = configuration["CloudflareR2:BucketName"];
            _publicUrl = configuration["CloudflareR2:PublicUrl"]?.TrimEnd('/');

            var accessKey = configuration["CloudflareR2:AccessKey"];
            var secretKey = configuration["CloudflareR2:SecretKey"];
            var serviceUrl = configuration["CloudflareR2:ServiceUrl"];

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
                    // CORRECT LOCATION: Disable payload signing on the upload request for Cloudflare R2 compatibility
                    DisablePayloadSigning = true 
                };

                await fileTransferUtility.UploadAsync(uploadRequest);
            }

            return $"{_publicUrl}/{fileName}";
        }
    }
}