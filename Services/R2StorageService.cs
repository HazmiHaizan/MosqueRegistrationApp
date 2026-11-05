using Amazon.S3;
using Amazon.S3.Model;
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
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _publicUrl;

        public R2StorageService(IConfiguration configuration)
        {
            var serviceUrl = configuration["R2:ServiceUrl"];
            var accessKey = configuration["R2:AccessKey"];
            var secretKey = configuration["R2:SecretKey"];
            _bucketName = configuration["R2:BucketName"];
            _publicUrl = configuration["R2:PublicUrl"];

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true
            };

            // Use AmazonS3Client instead of AmazonS3ServiceClient
            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            using (var stream = file.OpenReadStream())
            {
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = uniqueFileName,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                await _s3Client.PutObjectAsync(request);
            }

            return $"{_publicUrl}/{uniqueFileName}";
        }
    }
}