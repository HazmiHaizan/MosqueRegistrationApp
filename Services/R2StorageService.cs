using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MosqueRegistrationApp.Services
{
    public interface IR2StorageService
    {
        Task<string> UploadFileAsync(IFormFile file);
        Task DeleteFileAsync(string fileUrl); // MUST be declared here so DeleteUser can call it
    }

    public class R2StorageService : IR2StorageService
    {
        private readonly string _bucketName;
        private readonly string _publicUrl;
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<R2StorageService> _logger;

        public R2StorageService(IConfiguration configuration, ILogger<R2StorageService> logger)
        {
            _logger = logger;

            var serviceUrl = configuration["CloudflareR2__ServiceUrl"] 
                          ?? configuration["CloudflareR2:ServiceUrl"] 
                          ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_SERVICE_URL");

            var accessKey = configuration["CloudflareR2__AccessKey"] 
                         ?? configuration["CloudflareR2:AccessKey"] 
                         ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_ACCESS_KEY");

            var secretKey = configuration["CloudflareR2__SecretKey"] 
                         ?? configuration["CloudflareR2:SecretKey"] 
                         ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_SECRET_KEY");

            _bucketName = configuration["CloudflareR2__BucketName"] 
                       ?? configuration["CloudflareR2:BucketName"] 
                       ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_BUCKET_NAME") 
                       ?? "maes24-proofs";

            _publicUrl = (configuration["CloudflareR2__PublicUrl"] 
                       ?? configuration["CloudflareR2:PublicUrl"] 
                       ?? Environment.GetEnvironmentVariable("CLOUDFLARE_R2_PUBLIC_URL"))?.TrimEnd('/');

            _logger.LogWarning("================ R2 DIAGNOSTICS ================");
            _logger.LogWarning("ServiceUrl resolved: {Url}", string.IsNullOrEmpty(serviceUrl) ? "MISSING / NULL" : serviceUrl);
            _logger.LogWarning("BucketName resolved: {Bucket}", _bucketName);
            _logger.LogWarning("================================================");

            if (string.IsNullOrEmpty(serviceUrl))
            {
                throw new InvalidOperationException("CRITICAL: R2 ServiceURL environment variable is missing on Render!");
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

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            try
            {
                var uri = new Uri(fileUrl);
                var objectKey = Path.GetFileName(uri.AbsolutePath);

                if (!string.IsNullOrEmpty(objectKey))
                {
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = objectKey
                    };

                    await _s3Client.DeleteObjectAsync(deleteRequest);
                    _logger.LogInformation("Successfully deleted file {Key} from R2 bucket {Bucket}", objectKey, _bucketName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from R2 bucket for URL: {Url}", fileUrl);
            }
        }
    }
}